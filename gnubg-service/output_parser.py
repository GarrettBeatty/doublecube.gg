"""
Parse GNU Backgammon (gnubg) text output.
Ported from Backgammon.Analysis/Gnubg/GnubgOutputParser.cs
"""

import logging
import re
from dataclasses import dataclass
from typing import Optional

logger = logging.getLogger(__name__)


@dataclass
class PositionEvaluation:
    equity: float = 0.0
    win_prob: float = 0.0
    gammon_prob: float = 0.0
    bg_prob: float = 0.0


@dataclass
class MoveAnalysis:
    rank: int
    notation: str
    equity: float


@dataclass
class CubeDecision:
    no_double_eq: float = 0.0
    double_take_eq: float = 0.0
    double_pass_eq: float = 0.0
    recommendation: str = "NoDouble"
    details: Optional[str] = None


def parse_evaluation(gnubg_output: str) -> PositionEvaluation:
    """
    Parse position evaluation from gnubg output.

    Example gnubg eval output:
        Equity: +0.234
        Win: 56.2%
        Win G: 12.3%
        Win BG: 0.8%
    """
    evaluation = PositionEvaluation()

    # Parse equity - look for patterns like "Equity: +0.234" or "Equity  0.234"
    equity_match = re.search(r'Equity[:\s]+([-+]?\d+\.\d+)', gnubg_output, re.IGNORECASE)
    if equity_match:
        evaluation.equity = float(equity_match.group(1))

    # Parse win probability - look for "Win:" followed by percentage
    win_match = re.search(r'Win[:\s]+([\d.]+)%', gnubg_output, re.IGNORECASE)
    if win_match:
        evaluation.win_prob = float(win_match.group(1)) / 100.0

    # Parse gammon probability - look for "Win G:" or "Gammon:"
    gammon_match = re.search(r'(?:Win\s+G|Gammon)[:\s]+([\d.]+)%', gnubg_output, re.IGNORECASE)
    if gammon_match:
        evaluation.gammon_prob = float(gammon_match.group(1)) / 100.0

    # Parse backgammon probability - look for "Win BG:" or "Backgammon:"
    bg_match = re.search(r'(?:Win\s+BG|Backgammon)[:\s]+([\d.]+)%', gnubg_output, re.IGNORECASE)
    if bg_match:
        evaluation.bg_prob = float(bg_match.group(1)) / 100.0

    return evaluation


def parse_move_analysis(gnubg_output: str) -> list[MoveAnalysis]:
    """
    Parse move analysis from gnubg hint output.

    Example gnubg hint output:
        1. Cubeful 2-ply    8/5 8/4                      Eq.: +0.200
           0.571 0.000 0.000 - 0.429 0.000 0.000
        2. Cubeful 2-ply    8/4 6/3                      Eq.: +0.177 (-0.023)
           0.565 0.000 0.000 - 0.435 0.000 0.000
    """
    logger.info("=== PARSING GNUBG OUTPUT ===")
    logger.info("Raw output:\n%s", gnubg_output)

    move_analyses = []

    lines = gnubg_output.split('\n')
    logger.debug("Split into %d lines", len(lines))

    for line in lines:
        trimmed = line.strip()

        # Look for lines starting with a number followed by a period (move rank)
        if not trimmed or not trimmed[0].isdigit():
            if trimmed:
                logger.debug("Skipping non-move line: '%s'", trimmed[:50] if len(trimmed) > 50 else trimmed)
            continue

        dot_index = trimmed.find('.')
        if dot_index == -1:
            logger.debug("Skipping line (no dot): '%s'", trimmed[:50])
            continue

        # Parse rank
        try:
            rank = int(trimmed[:dot_index])
            logger.debug("Found rank %d in line: '%s'", rank, trimmed[:80])
        except ValueError:
            logger.debug("Skipping line (rank parse failed): '%s'", trimmed[:50])
            continue

        # Find equity marker "Eq.:"
        equity_index = trimmed.lower().find("eq.:")
        if equity_index == -1:
            logger.debug("Skipping rank %d - no 'Eq.:' found in: '%s'", rank, trimmed[:80])
            continue

        # Extract the section between rank and equity (contains move notation)
        move_section = trimmed[dot_index + 1:equity_index].strip()
        logger.debug("Rank %d move_section: '%s'", rank, move_section)

        # Remove evaluation type prefix (Cubeful/Cubeless N-ply)
        # and collect move notation (format: "N/N N/N")
        notation_parts = []
        for part in move_section.split():
            if '/' in part:
                notation_parts.append(part)
            else:
                logger.debug("Rank %d skipping part without '/': '%s'", rank, part)

        notation = ' '.join(notation_parts)
        logger.debug("Rank %d notation_parts: %s -> notation: '%s'", rank, notation_parts, notation)

        if not notation:
            logger.warning("Skipping rank %d - no notation found in move_section: '%s'", rank, move_section)
            continue

        # Parse equity value after "Eq.:"
        equity_section = trimmed[equity_index + 4:].strip()
        logger.debug("Rank %d equity_section: '%s'", rank, equity_section)

        # Extract first number (may have +/- sign)
        equity_match = re.match(r'([-+]?\d+\.?\d*)', equity_section)
        if not equity_match:
            logger.warning("Skipping rank %d - no equity match in: '%s'", rank, equity_section)
            continue

        try:
            equity = float(equity_match.group(1))
        except ValueError:
            logger.warning("Skipping rank %d - equity parse failed: '%s'", rank, equity_match.group(1))
            continue

        logger.info("Parsed move: rank=%d, notation='%s', equity=%.4f", rank, notation, equity)
        move_analyses.append(MoveAnalysis(
            rank=rank,
            notation=notation,
            equity=equity
        ))

    logger.info("=== PARSING COMPLETE: %d moves found ===", len(move_analyses))
    return move_analyses


def parse_cube_decision(gnubg_output: str) -> CubeDecision:
    """
    Parse cube decision from gnubg output.
    """
    decision = CubeDecision()
    decision.details = gnubg_output

    # Parse "No double" equity
    no_double_match = re.search(r'No\s+double[:\s]+([-+]?\d+\.\d+)', gnubg_output, re.IGNORECASE)
    if no_double_match:
        decision.no_double_eq = float(no_double_match.group(1))

    # Parse "Double, take" equity
    double_take_match = re.search(r'Double,\s+take[:\s]+([-+]?\d+\.\d+)', gnubg_output, re.IGNORECASE)
    if double_take_match:
        decision.double_take_eq = float(double_take_match.group(1))

    # Parse "Double, pass" equity
    double_pass_match = re.search(r'Double,\s+pass[:\s]+([-+]?\d+\.\d+)', gnubg_output, re.IGNORECASE)
    if double_pass_match:
        decision.double_pass_eq = float(double_pass_match.group(1))

    # Determine recommendation based on equities
    if (decision.double_take_eq > decision.no_double_eq and
        decision.double_take_eq > decision.double_pass_eq):
        decision.recommendation = "Double"
    elif (decision.double_pass_eq > decision.no_double_eq and
          decision.double_pass_eq > decision.double_take_eq):
        decision.recommendation = "TooGood"
    else:
        decision.recommendation = "NoDouble"

    # Look for explicit recommendation in output
    if "correct cube action: double" in gnubg_output.lower():
        decision.recommendation = "Double"
    elif "correct cube action: no double" in gnubg_output.lower():
        decision.recommendation = "NoDouble"
    elif "too good" in gnubg_output.lower():
        decision.recommendation = "TooGood"

    return decision
