# GNU Backgammon (gnubg) Setup Guide

This guide explains how to set up and use GNU Backgammon for advanced position analysis in your Backgammon application.

## Overview

GNU Backgammon (gnubg) is a world-class backgammon engine that provides highly accurate position evaluation using neural networks. This integration allows your application to analyze positions with professional-level accuracy.

## Docker Setup (Recommended for EC2)

The Backgammon.Server Dockerfile has been updated to include gnubg. It now uses a Debian-based image instead of Alpine to support gnubg installation.

### Building the Docker Image

From the project root, build the server image:

```bash
docker build -t backgammon-server:latest -f Backgammon.Server/Dockerfile .
```

### Verifying gnubg Installation

Test that gnubg is available in the container:

```bash
docker run --rm backgammon-server:latest gnubg --version
```

You should see output like:
```
GNU Backgammon 1.x.xxx
```

## Configuration

### Enable gnubg Analysis

Update your `appsettings.json` or environment variables to use the gnubg evaluator:

```json
{
  "Analysis": {
    "EvaluatorType": "Gnubg",
    "AllowUserSelection": false
  },
  "Gnubg": {
    "ExecutablePath": "gnubg",
    "TimeoutMs": 30000,
    "VerboseLogging": false,
    "EvaluationPlies": 2,
    "UseNeuralNet": true
  }
}
```

### Configuration Options

**Analysis Settings:**
- `EvaluatorType`: Set to `"Gnubg"` to use GNU Backgammon, or `"Heuristic"` for the faster built-in evaluator
- `AllowUserSelection`: Whether users can switch evaluators (future feature)

**Gnubg Settings:**
- `ExecutablePath`: Path to gnubg binary (default: `"gnubg"` assumes it's in PATH)
- `TimeoutMs`: Maximum time in milliseconds for gnubg operations (default: 30000)
- `VerboseLogging`: Enable detailed gnubg output for debugging (default: false)
- `EvaluationPlies`: Analysis depth (0-3+):
  - 0 = 0-ply (very fast, less accurate)
  - 1 = 1-ply (fast)
  - 2 = 2-ply (standard, recommended)
  - 3+ = very slow but most accurate
- `UseNeuralNet`: Enable neural network evaluation (default: true, recommended)

### Environment Variables (Alternative)

For Docker/EC2 deployments, you can also configure via environment variables:

```bash
export Analysis__EvaluatorType=Gnubg
export Gnubg__ExecutablePath=gnubg
export Gnubg__TimeoutMs=30000
export Gnubg__VerboseLogging=false
export Gnubg__EvaluationPlies=2
export Gnubg__UseNeuralNet=true
```

## Local Development (Non-Docker)

If you're running the server locally without Docker, install gnubg:

### Ubuntu/Debian
```bash
sudo apt-get update
sudo apt-get install gnubg
```

### macOS (Homebrew)
```bash
brew install gnubg
```

### Verify Installation
```bash
gnubg --version
```

## Deployment to EC2

### Option 1: Docker Deployment

1. Build the Docker image locally or on EC2:
```bash
docker build -t backgammon-server:latest -f Backgammon.Server/Dockerfile .
```

2. Run the container with environment variables:
```bash
docker run -d \
  -p 5000:5000 \
  -e Analysis__EvaluatorType=Gnubg \
  -e Gnubg__EvaluationPlies=2 \
  --name backgammon-server \
  backgammon-server:latest
```

### Option 2: Direct Installation on EC2

1. SSH into your EC2 instance

2. Install gnubg:
```bash
sudo apt-get update
sudo apt-get install -y gnubg
```

3. Verify installation:
```bash
gnubg --version
```

4. Update your application's `appsettings.json` to enable gnubg (see Configuration section above)

## Testing the Integration

### 1. Check Application Logs

When the server starts, you should see logs indicating which evaluator is being used:

```
info: Backgammon.Server.Services.AnalysisService[0]
      Using Gnubg position evaluator
```

### 2. Test Analysis via API

Make a game move and check that position analysis is returned. The analysis should include equity values and win percentages calculated by gnubg.

### 3. Enable Verbose Logging

For debugging, set `Gnubg.VerboseLogging = true` in your configuration. This will log all gnubg commands and outputs.

## Performance Considerations

- **Evaluation Plies**: Higher ply counts are exponentially slower. For real-time analysis during games, 2-ply is recommended.
- **Timeout**: Adjust `TimeoutMs` based on your server's performance. Slower servers may need higher timeouts.
- **Concurrency**: The `GnubgProcessManager` uses a semaphore to ensure only one gnubg process runs at a time, preventing resource exhaustion.

## Troubleshooting

### "gnubg not found" Error

**Cause**: The gnubg executable is not in PATH or doesn't exist at the specified location.

**Solution**:
- For Docker: Rebuild the image to ensure gnubg is installed
- For local/EC2: Install gnubg using your package manager
- Check the `ExecutablePath` configuration

### Timeout Errors

**Cause**: gnubg is taking too long to analyze positions.

**Solution**:
- Reduce `EvaluationPlies` (try 1 or 0)
- Increase `TimeoutMs`
- Check server CPU performance

### Fallback to Heuristic Evaluator

If gnubg fails to start or is unavailable, the application will fall back to the built-in heuristic evaluator and log a warning. This ensures games can continue even if gnubg is not working.

## Image Size Impact

Switching from Alpine to Debian increases the base image size:
- Alpine runtime: ~110 MB
- Debian runtime with gnubg: ~250-300 MB

This is acceptable for EC2 deployments where disk space is not a constraint. The benefit of professional-level game analysis outweighs the image size increase.

## Next Steps

1. Build and deploy the updated Docker image
2. Update your configuration to enable gnubg
3. Monitor logs to ensure gnubg is working correctly
4. Adjust `EvaluationPlies` based on your performance requirements

## Additional Resources

- [GNU Backgammon Official Site](https://www.gnu.org/software/gnubg/)
- [GNU Backgammon Manual](https://www.gnu.org/software/gnubg/manual/gnubg.html)
- [Backgammon Analysis Configuration](../Backgammon.Server/Configuration/GnubgSettings.cs)
