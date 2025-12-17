# Browser-Based Game Deployment Guide

This document describes how to deploy and use the browser-based version of the Andastra game.

## Architecture

The browser-based game consists of two main components:

### Backend (ASP.NET Core Web API)
- **Technology**: ASP.NET Core 9.0 with SignalR
- **Purpose**: Hosts the game server-side and manages game sessions
- **Port**: 5000 (default)
- **Features**:
  - Real-time game state streaming via SignalR
  - Game session management
  - Input processing from web clients
  - RESTful API for game information

### Frontend (Blazor WebAssembly)
- **Technology**: Blazor WebAssembly with SignalR client
- **Purpose**: Provides the browser-based UI for playing the game
- **Port**: 8080 (default, served via nginx)
- **Features**:
  - Real-time game rendering in browser
  - Keyboard and mouse input capture
  - SignalR connection to backend
  - Responsive game interface

## Quick Start with Docker

The easiest way to run the browser-based game is using Docker Compose:

### Prerequisites
- Docker installed (version 20.10 or later)
- Docker Compose installed (version 2.0 or later)

### Build and Run

```bash
# Build and start the services
docker-compose up --build

# Or run in detached mode
docker-compose up --build -d
```

### Access the Game

Once running, open your browser and navigate to:
- **Game UI**: http://localhost:8080
- **Backend API**: http://localhost:5000

### Stop the Services

```bash
# Stop the services
docker-compose down

# Stop and remove volumes
docker-compose down -v
```

## Manual Build (Without Docker)

### Backend

```bash
# Navigate to backend project
cd src/Andastra.Web.Backend

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the backend
dotnet run
```

The backend will be available at http://localhost:5000

### Frontend

```bash
# Navigate to frontend project
cd src/Andastra.Web.Frontend

# Restore dependencies
dotnet restore

# Build and run the frontend
dotnet run
```

The frontend will typically be available at http://localhost:5001

**Note**: When running manually, you may need to update the SignalR connection URL in the frontend code to match your backend URL.

## How to Play

1. Navigate to the frontend URL (http://localhost:8080 if using Docker)
2. Click on "Game" in the navigation menu
3. Click "Start New Game" to create a new game session
4. The game will connect to the backend and start streaming frames
5. Use keyboard and mouse to interact with the game
6. Click "Stop Game" to end the session

## Configuration

### Backend Configuration

Edit `src/Andastra.Web.Backend/appsettings.json` to configure:
- Logging levels
- CORS policies
- SignalR settings

### Frontend Configuration

The frontend connects to the backend via SignalR. The connection URL is configured in `Game.razor`.

To change the backend URL, modify the `HubConnectionBuilder` configuration:

```csharp
hubConnection = new HubConnectionBuilder()
    .WithUrl("http://your-backend-url:5000/gamehub")
    .WithAutomaticReconnect()
    .Build();
```

## Docker Deployment Options

### Build Individual Images

```bash
# Build backend image
docker build -f Dockerfile.backend -t andastra-backend .

# Build frontend image
docker build -f Dockerfile.frontend -t andastra-frontend .

# Run backend
docker run -d -p 5000:5000 --name andastra-backend andastra-backend

# Run frontend
docker run -d -p 8080:80 --name andastra-frontend andastra-frontend
```

### Environment Variables

You can customize the deployment using environment variables:

```bash
# Set custom port for backend
docker-compose up -e BACKEND_PORT=5001

# Or edit docker-compose.yml to change defaults
```

## Production Deployment

For production deployment, consider:

1. **SSL/TLS**: Configure HTTPS for both frontend and backend
2. **Load Balancing**: Use a load balancer for multiple backend instances
3. **Reverse Proxy**: Use nginx or similar for SSL termination and routing
4. **Monitoring**: Add application monitoring and health checks
5. **Resource Limits**: Configure Docker resource limits for stability
6. **Secrets Management**: Use proper secrets management for any API keys or credentials

### Example Production docker-compose.yml

```yaml
version: '3.8'

services:
  backend:
    build:
      context: .
      dockerfile: Dockerfile.backend
    ports:
      - "5000:5000"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:5000
    restart: always
    deploy:
      resources:
        limits:
          cpus: '2'
          memory: 2G
        reservations:
          cpus: '1'
          memory: 1G

  frontend:
    build:
      context: .
      dockerfile: Dockerfile.frontend
    ports:
      - "8080:80"
    depends_on:
      - backend
    restart: always
    deploy:
      resources:
        limits:
          cpus: '1'
          memory: 512M
```

## Troubleshooting

### Connection Issues

If the frontend cannot connect to the backend:

1. Check that both services are running
2. Verify the backend URL in the frontend configuration
3. Check CORS settings in the backend
4. Verify network connectivity between containers (if using Docker)

### Performance Issues

If game performance is poor:

1. Check backend resource usage
2. Verify network latency between frontend and backend
3. Consider reducing frame rate in `GameSessionManager.cs`
4. Check browser console for errors

### Docker Issues

```bash
# Check container logs
docker-compose logs backend
docker-compose logs frontend

# Check container status
docker-compose ps

# Restart services
docker-compose restart
```

## API Endpoints

### Backend REST API

- `GET /api/game/health` - Health check endpoint
- `GET /api/game/session/{sessionId}` - Get session information

### SignalR Hub

- Endpoint: `/gamehub`
- Methods:
  - `CreateSession()` - Create a new game session
  - `JoinSession(sessionId)` - Join an existing session
  - `SendInput(sessionId, input)` - Send player input
- Events:
  - `ReceiveFrame(frameData)` - Receive game frame updates

## Future Enhancements

Potential improvements for the browser-based game:

- [ ] WebGL rendering for better performance
- [ ] WebRTC for lower-latency streaming
- [ ] Multiplayer support
- [ ] Save/load game state in browser
- [ ] Mobile touch controls
- [ ] Audio streaming
- [ ] Full-screen mode
- [ ] Game settings UI

## License

This project is licensed under the Business Source License 1.1 (BSL-1.1). See the LICENSE file for details.
