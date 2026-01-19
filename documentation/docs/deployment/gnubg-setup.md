---
sidebar_position: 3
---

# GNU Backgammon Setup

Configure GNU Backgammon (gnubg) for expert-level position analysis.

## Overview

GNU Backgammon is a world-class backgammon engine using neural networks for position evaluation. Integration enables professional-level move analysis.

## Docker Setup (Recommended)

The server Dockerfile includes gnubg automatically.

### Build the Image

```bash
docker build -t backgammon-server:latest -f Backgammon.Server/Dockerfile .
```

### Verify Installation

```bash
docker run --rm backgammon-server:latest gnubg --version
```

Expected output:
```
GNU Backgammon 1.x.xxx
```

## Configuration

### Application Settings

Update `appsettings.json`:

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

| Setting | Description | Default |
|---------|-------------|---------|
| `EvaluatorType` | `Gnubg` or `Heuristic` | `Heuristic` |
| `ExecutablePath` | Path to gnubg binary | `gnubg` |
| `TimeoutMs` | Max operation time (ms) | `30000` |
| `VerboseLogging` | Enable debug output | `false` |
| `EvaluationPlies` | Analysis depth (0-3+) | `2` |
| `UseNeuralNet` | Use neural network | `true` |

### Evaluation Plies

| Ply | Speed | Accuracy | Use Case |
|-----|-------|----------|----------|
| 0 | Very fast | Lower | Quick hints |
| 1 | Fast | Good | Real-time |
| 2 | Moderate | Very good | Recommended |
| 3+ | Slow | Excellent | Post-game analysis |

### Environment Variables

Alternative configuration via environment:

```bash
export Analysis__EvaluatorType=Gnubg
export Gnubg__ExecutablePath=gnubg
export Gnubg__TimeoutMs=30000
export Gnubg__EvaluationPlies=2
```

## Local Development

### Ubuntu/Debian

```bash
sudo apt-get update
sudo apt-get install gnubg
```

### macOS

```bash
brew install gnubg
```

### Verify

```bash
gnubg --version
```

## Production Deployment

### Docker Deployment

```bash
docker run -d \
  -p 5000:5000 \
  -e Analysis__EvaluatorType=Gnubg \
  -e Gnubg__EvaluationPlies=2 \
  --name backgammon-server \
  backgammon-server:latest
```

### EC2 Direct Installation

```bash
sudo apt-get update
sudo apt-get install -y gnubg
gnubg --version
```

## Verification

### Check Application Logs

On startup, you should see:

```
info: Backgammon.Server.Services.AnalysisService[0]
      Using Gnubg position evaluator
```

### Test Analysis API

```bash
curl -X POST https://api.doublecube.gg/api/analyze \
  -H "Authorization: Bearer <token>" \
  -d '{"gameId": "xxx"}'
```

## Fallback Behavior

If gnubg is unavailable:
- Application falls back to heuristic evaluator
- Warning logged
- Games continue uninterrupted

## Troubleshooting

### "gnubg not found"

**Cause**: Binary not in PATH or not installed.

**Solution**:
- Rebuild Docker image
- Install via package manager
- Check `ExecutablePath` setting

### Timeout Errors

**Cause**: Analysis taking too long.

**Solution**:
- Reduce `EvaluationPlies` to 1 or 0
- Increase `TimeoutMs`
- Check server CPU load

### High Memory Usage

gnubg loads neural network weights into memory (~100-200 MB).

The `GnubgProcessManager` uses a semaphore to limit concurrent processes.

## Performance Notes

- Higher ply = exponentially slower
- 2-ply recommended for real-time analysis
- 3+ ply for post-game deep analysis
- Image size increases ~150 MB vs Alpine base
