# GNU Backgammon (gnubg) Setup Guide

This guide explains how to install and configure GNU Backgammon (gnubg) for use with the Backgammon application's analysis features.

## Overview

GNU Backgammon is a professional-grade backgammon engine that provides world-class position evaluation and move analysis. This application can use gnubg as an alternative to the built-in heuristic evaluator, offering significantly more accurate analysis.

## Installation

### macOS

#### Option 1: Homebrew Cask (Recommended)

```bash
brew install --cask gnubg
```

**Note**: gnubg requires installation via Homebrew Cask. Due to hard-coded paths, it must be installed in `/Applications`:

```bash
brew install --cask --appdir=/Applications gnubg
```

#### Option 2: MacPorts

```bash
sudo port install gnubg
```

#### Option 3: Build from Source

Download from [GNU Backgammon](https://www.gnu.org/software/gnubg/) or [gnubg.org](http://www.gnubg.org/xml-rss2.php?catid=10):
```bash
# Download gnubg-release-1.08.003-sources.tar.gz
tar -xzf gnubg-release-1.08.003-sources.tar.gz
cd gnubg-1.08.003
./configure
make
sudo make install
```

### Linux

#### Debian/Ubuntu

GNU Backgammon is available in the Ubuntu universe repository:

```bash
sudo apt-get update
sudo apt-get install gnubg
```

**Note**: The package includes both the gnubg binary and data files. No separate `gnubg-data` package needed on recent versions.

#### Fedora/RHEL/CentOS

```bash
sudo dnf install gnubg
```

#### Arch Linux

```bash
sudo pacman -S gnubg
```

#### Build from Source (All Linux)

If your distribution doesn't have a package:

```bash
# Install prerequisites (Debian/Ubuntu)
sudo apt-get install build-essential libglib2.0-dev

# Download and build
wget http://www.gnubg.org/media/sources/gnubg-release-1.08.003-sources.tar.gz
tar -xzf gnubg-release-1.08.003-sources.tar.gz
cd gnubg-1.08.003
./configure
make
sudo make install
```

**Prerequisites**: GLib version 2.8 or higher is required.

### Windows

1. Download the Windows installer: [gnubg-1_08_003-20240428-setup.exe](http://gnubg.org/win32/GNUBgW.htm)
2. Run the installer and follow the installation wizard
3. Default installation path: `C:\Program Files\gnubg\`
4. Add the installation directory to your PATH, or note the full path to `gnubg.exe`

## Verification

After installation, verify gnubg is working:

```bash
gnubg --version
```

You should see output similar to:

```
GNU Backgammon X.X.X
```

Test text mode (required for API integration):

```bash
gnubg -t
```

You should enter gnubg's command-line interface. Type `quit` to exit.

## Configuration

### Development (Local)

1. Open `Backgammon.Server/appsettings.Development.json`

2. Set the evaluator type to use gnubg:

```json
{
  "Analysis": {
    "EvaluatorType": "Gnubg"
  },
  "Gnubg": {
    "ExecutablePath": "gnubg",
    "VerboseLogging": true
  }
}
```

#### Platform-Specific Paths

If gnubg is not in your PATH, specify the full path:

**macOS (Homebrew):**
```json
"ExecutablePath": "/usr/local/bin/gnubg"
```

**macOS (MacPorts):**
```json
"ExecutablePath": "/opt/local/bin/gnubg"
```

**Linux:**
```json
"ExecutablePath": "/usr/bin/gnubg"
```

**Windows:**
```json
"ExecutablePath": "C:\\Program Files\\gnubg\\gnubg.exe"
```

### Production

1. Open `Backgammon.Server/appsettings.Production.json`

2. Configure gnubg settings:

```json
{
  "Analysis": {
    "EvaluatorType": "Gnubg"
  },
  "Gnubg": {
    "ExecutablePath": "/usr/bin/gnubg",
    "TimeoutMs": 30000,
    "VerboseLogging": false,
    "EvaluationPlies": 2,
    "UseNeuralNet": true
  }
}
```

### Configuration Options

| Setting | Description | Default | Recommended |
|---------|-------------|---------|-------------|
| `EvaluatorType` | Which evaluator to use: "Heuristic" or "Gnubg" | "Heuristic" | "Gnubg" (if installed) |
| `ExecutablePath` | Path to gnubg executable | "gnubg" | System-specific (see above) |
| `TimeoutMs` | Maximum time for gnubg operations (milliseconds) | 30000 | 30000-60000 |
| `VerboseLogging` | Enable detailed gnubg logging | false | true (dev), false (prod) |
| `EvaluationPlies` | Analysis depth (0-3+) | 2 | 2 (standard), 3 (slow but accurate) |
| `UseNeuralNet` | Enable neural network evaluation | true | true |

### Evaluation Plies Explained

- **0-ply**: Fast, pattern-based evaluation (~instant)
- **1-ply**: One move lookahead (~0.5s per position)
- **2-ply**: Two moves lookahead (~5s per position) - **Recommended**
- **3-ply**: Three moves lookahead (~60s per position) - Very slow

## Running the Application

### Development with Aspire

```bash
cd Backgammon.AppHost
dotnet run
```

The application will:
1. Check if gnubg is available
2. If available, use GnubgEvaluator for analysis
3. If not available, automatically fall back to HeuristicEvaluator with a warning

### Development without Aspire

Terminal 1 (Server):
```bash
cd Backgammon.Server
dotnet run
```

Terminal 2 (Client):
```bash
cd Backgammon.WebClient
npm run dev
```

## Testing the Integration

1. Start the application
2. Navigate to the Analysis page
3. Create or import a position
4. Click "Analyze Position"
5. Check the evaluator badge - it should display "GNU Backgammon"

## Troubleshooting

### Error: "Gnubg not available"

**Check Installation:**
```bash
which gnubg  # macOS/Linux
where gnubg  # Windows
```

**Check Permissions:**
```bash
ls -l $(which gnubg)  # Should be executable
chmod +x /path/to/gnubg  # If needed
```

### Error: "Gnubg process timed out"

Increase the timeout in configuration:

```json
{
  "Gnubg": {
    "TimeoutMs": 60000
  }
}
```

Or reduce evaluation plies:

```json
{
  "Gnubg": {
    "EvaluationPlies": 1
  }
}
```

### Error: "Failed to parse gnubg output"

This may indicate:
1. Incompatible gnubg version (try 1.06.002 or newer)
2. Gnubg weights/database files missing

**Fix missing weights:**
```bash
# Debian/Ubuntu
sudo apt-get install gnubg-data

# macOS Homebrew
brew reinstall gnubg
```

### Slow Analysis

If analysis is too slow:

1. **Reduce plies:**
   ```json
   "EvaluationPlies": 1
   ```

2. **Use 0-ply for fast approximate analysis:**
   ```json
   "EvaluationPlies": 0
   ```

3. **Increase timeout:**
   ```json
   "TimeoutMs": 60000
   ```

### Fallback to Heuristic Evaluator

The application automatically falls back to the built-in HeuristicEvaluator if:
- Gnubg is not installed
- Gnubg executable not found at configured path
- Gnubg fails availability check

Check the server logs for messages like:
```
Gnubg not available at /usr/bin/gnubg, falling back to HeuristicEvaluator
Using HeuristicEvaluator for position analysis
```

## Docker Deployment

For containerized deployments, gnubg can be included in the Docker image:

### Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

# Install gnubg
RUN apt-get update && apt-get install -y \
    gnubg \
    gnubg-data \
    && rm -rf /var/lib/apt/lists/*

# Verify installation
RUN gnubg --version

# ... rest of Dockerfile
```

### Docker Compose

```yaml
services:
  backgammon-server:
    build: .
    environment:
      - Analysis__EvaluatorType=Gnubg
      - Gnubg__ExecutablePath=/usr/bin/gnubg
```

## Performance Considerations

### Concurrency

The application uses a semaphore to ensure only one gnubg process runs at a time. Multiple concurrent analysis requests will be queued.

### Caching

Position evaluations are not currently cached. For high-volume analysis, consider:
- Using HeuristicEvaluator for quick estimates
- Implementing application-level caching
- Running gnubg in a separate service with its own cache

### Scaling

For production deployments with heavy analysis load:
- Use HeuristicEvaluator for real-time in-game analysis
- Reserve gnubg for post-game analysis and learning features
- Consider running gnubg as a separate microservice

## Switching Between Evaluators

To switch between Heuristic and Gnubg evaluators:

1. Stop the application
2. Edit `appsettings.json` or `appsettings.Development.json`:
   ```json
   {
     "Analysis": {
       "EvaluatorType": "Heuristic"  // or "Gnubg"
     }
   }
   ```
3. Restart the application

No code changes or recompilation needed!

## Advanced Configuration

### Match Play Analysis

For match play with match equity table (MET) consideration:

```json
{
  "Gnubg": {
    "EvaluationPlies": 2,
    "UseNeuralNet": true
  }
}
```

Gnubg automatically uses METs when match score is available.

### Custom Gnubg Commands

The application uses these gnubg commands:

```bash
set automatic game off
set automatic roll off
import sgf <position>
set evaluation plies 2
eval
hint
```

To modify these commands, edit:
- `Backgammon.Analysis/Gnubg/GnubgCommandBuilder.cs`

## Resources

- [GNU Backgammon Official Website](https://www.gnubg.org/)
- [GNU Backgammon Documentation](https://www.gnubg.org/manual/)
- [GNU Backgammon Source Code](https://git.savannah.gnu.org/cgit/gnubg.git)

## Support

If you encounter issues with gnubg integration:

1. Check the server logs for detailed error messages
2. Verify gnubg works standalone: `gnubg -t`
3. Try the built-in HeuristicEvaluator as a fallback
4. Report issues at: https://github.com/yourusername/Backgammon/issues

## Future Enhancements

Planned features for gnubg integration:

- [ ] Cube decision analysis UI
- [ ] Full game review with gnubg
- [ ] Rollout analysis (evaluate all 21 dice rolls)
- [ ] Match equity table visualization
- [ ] Batch position analysis
- [ ] Gnubg settings in web UI
- [ ] Cached evaluations for common positions
- [ ] Gnubg process pooling for concurrency
