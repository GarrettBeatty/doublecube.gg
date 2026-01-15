FROM python:3.11-slim

# Install gnubg
RUN apt-get update && apt-get install -y --no-install-recommends \
    gnubg \
    && rm -rf /var/lib/apt/lists/*

# Add /usr/games to PATH (gnubg installs here on Debian)
ENV PATH="${PATH}:/usr/games"

# Verify gnubg installation
RUN gnubg --version

# Create gnubg configuration to disable sound (prevents ALSA errors in container)
RUN echo "set sound enable off" > /root/.gnubgrc

WORKDIR /app

# Install Python dependencies
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

# Copy application code
COPY . .

EXPOSE 8080

# Run the Flask app
CMD ["python", "app.py"]
