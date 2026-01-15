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
