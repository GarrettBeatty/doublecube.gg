"""
GNU Backgammon process management.
Spawns gnubg processes and executes commands.
"""

import os
import subprocess
import tempfile
import threading
import uuid
from typing import Optional


class GnubgRunner:
    """Manages gnubg process execution."""

    def __init__(self, executable: str = "gnubg", timeout_ms: int = 30000, verbose: bool = False):
        self.executable = executable
        self.timeout_s = timeout_ms / 1000.0
        self.verbose = verbose
        self._lock = threading.Lock()

    def is_available(self) -> bool:
        """Check if gnubg is available and working."""
        try:
            output = self.execute_commands(["show version"])
            return "gnu backgammon" in output.lower()
        except Exception:
            return False

    def execute_commands(self, commands: list[str]) -> str:
        """
        Execute gnubg commands and return the output.

        Args:
            commands: List of gnubg commands to execute

        Returns:
            Output from gnubg
        """
        with self._lock:
            # Add quit command at the end
            full_command = '\n'.join(commands) + '\nquit\n'

            if self.verbose:
                print(f"Executing gnubg commands:\n{full_command}")

            try:
                result = subprocess.run(
                    [self.executable, "-t"],  # -t for text mode (no GUI)
                    input=full_command,
                    capture_output=True,
                    text=True,
                    timeout=self.timeout_s
                )

                if self.verbose:
                    print(f"Gnubg output:\n{result.stdout}")
                    if result.stderr:
                        print(f"Gnubg stderr:\n{result.stderr}")

                if result.returncode != 0 and self.verbose:
                    print(f"Gnubg exited with code {result.returncode}")

                if not result.stdout and result.stderr:
                    raise Exception(f"Gnubg execution failed: {result.stderr}")

                return result.stdout

            except subprocess.TimeoutExpired:
                raise TimeoutError(f"Gnubg process timed out after {self.timeout_s}s")

    def execute_with_sgf(self, sgf_content: str, commands: list[str]) -> str:
        """
        Execute gnubg commands with an SGF position loaded from a temporary file.

        Args:
            sgf_content: SGF position content to load
            commands: List of gnubg commands to execute after loading position

        Returns:
            Output from gnubg
        """
        # Create temp file for SGF
        temp_file = os.path.join(tempfile.gettempdir(), f"gnubg_{uuid.uuid4()}.sgf")

        try:
            # Write SGF content to temp file
            with open(temp_file, 'w') as f:
                f.write(sgf_content)

            if self.verbose:
                print(f"Wrote SGF to temp file: {temp_file}")

            # Prepend load position command
            all_commands = [f"load position {temp_file}"] + commands

            return self.execute_commands(all_commands)

        finally:
            # Clean up temp file
            if os.path.exists(temp_file):
                try:
                    os.remove(temp_file)
                except Exception as e:
                    if self.verbose:
                        print(f"Failed to delete temp file {temp_file}: {e}")


def build_evaluation_commands(plies: int = 2, use_neural_net: bool = True) -> list[str]:
    """Build gnubg commands for position evaluation."""
    commands = [
        "set automatic game off",
        "set automatic roll off",
        f"set evaluation chequerplay evaluation plies {plies}",
        f"set evaluation cubedecision evaluation plies {plies}",
    ]

    if use_neural_net:
        commands.append("set evaluation chequerplay evaluation cubeful off")  # Faster for position eval

    commands.append("eval")
    return commands


def build_hint_commands(plies: int = 2, use_neural_net: bool = True) -> list[str]:
    """Build gnubg commands for finding best moves (hint)."""
    commands = [
        "set automatic game off",
        "set automatic roll off",
        f"set evaluation chequerplay evaluation plies {plies}",
        f"set evaluation cubedecision evaluation plies {plies}",
    ]

    if use_neural_net:
        commands.append("set evaluation chequerplay evaluation cubeful off")

    commands.append("hint")
    return commands


def build_cube_commands(plies: int = 2, use_neural_net: bool = True) -> list[str]:
    """Build gnubg commands for cube decision analysis."""
    commands = [
        "set automatic game off",
        "set automatic roll off",
        f"set evaluation chequerplay evaluation plies {plies}",
        f"set evaluation cubedecision evaluation plies {plies}",
    ]

    if use_neural_net:
        commands.append("set evaluation chequerplay evaluation cubeful on")  # Important for cube
        commands.append("set evaluation cubedecision evaluation cubeful on")

    commands.append("hint cube")
    return commands
