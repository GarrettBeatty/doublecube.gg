"""
GNU Backgammon HTTP Service.
Provides REST API endpoints for position evaluation, move hints, and cube decisions.
"""

import os
from dataclasses import asdict
from flask import Flask, request, jsonify

from gnubg_runner import (
    GnubgRunner,
    build_evaluation_commands,
    build_hint_commands,
    build_cube_commands
)
from output_parser import (
    parse_evaluation,
    parse_move_analysis,
    parse_cube_decision
)

app = Flask(__name__)

# Configuration from environment
GNUBG_EXECUTABLE = os.environ.get("GNUBG_EXECUTABLE", "gnubg")
GNUBG_TIMEOUT_MS = int(os.environ.get("GNUBG_TIMEOUT_MS", "30000"))
GNUBG_VERBOSE = os.environ.get("GNUBG_VERBOSE", "false").lower() == "true"

# Create gnubg runner instance
runner = GnubgRunner(
    executable=GNUBG_EXECUTABLE,
    timeout_ms=GNUBG_TIMEOUT_MS,
    verbose=GNUBG_VERBOSE
)


@app.route("/health", methods=["GET"])
def health():
    """Health check endpoint."""
    gnubg_available = runner.is_available()
    return jsonify({
        "status": "ok" if gnubg_available else "degraded",
        "gnubg_available": gnubg_available
    })


@app.route("/evaluate", methods=["POST"])
def evaluate():
    """
    Evaluate a position.

    Request body:
        {
            "sgf": "(;FF[4]GM[6]...)",  // SGF position string
            "plies": 2                   // Optional, default 2
        }

    Response:
        {
            "equity": 0.234,
            "win_prob": 0.56,
            "gammon_prob": 0.12,
            "bg_prob": 0.01
        }
    """
    try:
        data = request.get_json()

        if not data or "sgf" not in data:
            return jsonify({"error": "Missing 'sgf' field"}), 400

        sgf = data["sgf"]
        plies = data.get("plies", 2)

        # Build commands and execute
        commands = build_evaluation_commands(plies=plies)
        output = runner.execute_with_sgf(sgf, commands)

        # Parse output
        evaluation = parse_evaluation(output)

        return jsonify(asdict(evaluation))

    except TimeoutError as e:
        return jsonify({"error": str(e)}), 504
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/hint", methods=["POST"])
def hint():
    """
    Get best move suggestions.

    Request body:
        {
            "sgf": "(;FF[4]GM[6]...)",  // SGF position string
            "plies": 2                   // Optional, default 2
        }

    Response:
        {
            "moves": [
                {"rank": 1, "notation": "8/5 8/4", "equity": 0.2},
                {"rank": 2, "notation": "8/4 6/3", "equity": 0.177},
                ...
            ]
        }
    """
    try:
        data = request.get_json()

        if not data or "sgf" not in data:
            return jsonify({"error": "Missing 'sgf' field"}), 400

        sgf = data["sgf"]
        plies = data.get("plies", 2)

        # Build commands and execute
        commands = build_hint_commands(plies=plies)
        output = runner.execute_with_sgf(sgf, commands)

        # Parse output
        moves = parse_move_analysis(output)

        return jsonify({
            "moves": [asdict(m) for m in moves]
        })

    except TimeoutError as e:
        return jsonify({"error": str(e)}), 504
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/cube", methods=["POST"])
def cube():
    """
    Analyze doubling cube decision.

    Request body:
        {
            "sgf": "(;FF[4]GM[6]...)",  // SGF position string
            "plies": 2                   // Optional, default 2
        }

    Response:
        {
            "no_double_eq": 0.23,
            "double_take_eq": 0.20,
            "double_pass_eq": 0.45,
            "recommendation": "Double"
        }
    """
    try:
        data = request.get_json()

        if not data or "sgf" not in data:
            return jsonify({"error": "Missing 'sgf' field"}), 400

        sgf = data["sgf"]
        plies = data.get("plies", 2)

        # Build commands and execute
        commands = build_cube_commands(plies=plies)
        output = runner.execute_with_sgf(sgf, commands)

        # Parse output
        decision = parse_cube_decision(output)

        # Don't include details in response (too verbose)
        result = asdict(decision)
        del result["details"]

        return jsonify(result)

    except TimeoutError as e:
        return jsonify({"error": str(e)}), 504
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/execute", methods=["POST"])
def execute():
    """
    Execute raw gnubg commands.

    Request body:
        {
            "commands": ["set board simple ...", "set dice 3 1", "hint"]
        }

    Response:
        {
            "output": "... raw gnubg output ..."
        }
    """
    try:
        data = request.get_json()

        if not data or "commands" not in data:
            return jsonify({"error": "Missing 'commands' field"}), 400

        commands = data["commands"]

        output = runner.execute_commands(commands)

        return jsonify({"output": output})

    except TimeoutError as e:
        return jsonify({"error": str(e)}), 504
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/hint-native", methods=["POST"])
def hint_native():
    """
    Get best move suggestions using native position format.

    Request body:
        {
            "position": "4HPwATDgc/ABMA",  // Position ID (Base64)
            "dice": [3, 1],
            "player": "O",  // O = player 0 (moves 24->1), X = player 1 (moves 1->24)
            "plies": 2
        }

    Response:
        {
            "moves": [
                {"rank": 1, "notation": "8/5 6/5", "equity": 0.2},
                ...
            ]
        }
    """
    try:
        data = request.get_json()

        if not data:
            return jsonify({"error": "Missing request body"}), 400

        position = data.get("position")
        dice = data.get("dice")
        player = data.get("player", "O")
        plies = data.get("plies", 2)

        if not position or not dice:
            return jsonify({"error": "Missing 'position' or 'dice' field"}), 400

        # Build commands using Position ID format (handles bar correctly)
        commands = [
            "set automatic game off",
            "set automatic roll off",
            f"set evaluation chequerplay evaluation plies {plies}",
            f"set evaluation cubedecision evaluation plies {plies}",
            "set evaluation chequerplay evaluation cubeful off",
            "new game",
            f"set board {position}",
            f"set dice {dice[0]} {dice[1]}",
            "hint"
        ]

        output = runner.execute_commands(commands)

        # Parse output
        moves = parse_move_analysis(output)

        return jsonify({
            "moves": [asdict(m) for m in moves]
        })

    except TimeoutError as e:
        return jsonify({"error": str(e)}), 504
    except Exception as e:
        return jsonify({"error": str(e)}), 500


@app.route("/eval-native", methods=["POST"])
def eval_native():
    """
    Evaluate position using native position format.

    Request body:
        {
            "position": "4HPwATDgc/ABMA",  // Position ID (Base64)
            "dice": [3, 1],
            "player": "O",
            "plies": 2
        }
    """
    try:
        data = request.get_json()

        if not data:
            return jsonify({"error": "Missing request body"}), 400

        position = data.get("position")
        dice = data.get("dice")
        player = data.get("player", "O")
        plies = data.get("plies", 2)

        if not position or not dice:
            return jsonify({"error": "Missing 'position' or 'dice' field"}), 400

        # Build commands using Position ID format (handles bar correctly)
        commands = [
            "set automatic game off",
            "set automatic roll off",
            f"set evaluation chequerplay evaluation plies {plies}",
            f"set evaluation cubedecision evaluation plies {plies}",
            "set evaluation chequerplay evaluation cubeful off",
            "new game",
            f"set board {position}",
            f"set dice {dice[0]} {dice[1]}",
            "eval"
        ]

        output = runner.execute_commands(commands)

        # Parse output
        evaluation = parse_evaluation(output)

        return jsonify(asdict(evaluation))

    except TimeoutError as e:
        return jsonify({"error": str(e)}), 504
    except Exception as e:
        return jsonify({"error": str(e)}), 500


if __name__ == "__main__":
    port = int(os.environ.get("PORT", "8080"))
    debug = os.environ.get("FLASK_DEBUG", "false").lower() == "true"

    print(f"Starting gnubg-service on port {port}")
    print(f"  Executable: {GNUBG_EXECUTABLE}")
    print(f"  Timeout: {GNUBG_TIMEOUT_MS}ms")
    print(f"  Verbose: {GNUBG_VERBOSE}")

    # Check gnubg availability on startup
    if runner.is_available():
        print("  gnubg: available")
    else:
        print("  gnubg: NOT AVAILABLE - service will return errors")

    app.run(host="0.0.0.0", port=port, debug=debug)
