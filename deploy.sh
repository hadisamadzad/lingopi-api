#!/bin/sh

set -eu

# Require an environment argument.
if [ "$#" -lt 1 ] || [ -z "$1" ]; then
    echo "Usage: $0 <environment>" >&2
    exit 1
fi

ENV=$1
APP_VERSION=${APP_VERSION:-unknown}
GIT_SHA=${GIT_SHA:-unknown}

echo "Starting deployment with environment: $ENV"

# Keep Compose file options in the positional parameters so this script works
# with both POSIX sh (Ubuntu's default /bin/sh) and Bash.
set -- -f docker-compose.yml
if [ "$ENV" = "Local" ]; then
    set -- "$@" -f docker-compose.local.yml
fi

# Stop and remove existing containers first
echo "Stopping existing containers..."
ENV="$ENV" APP_VERSION="$APP_VERSION" GIT_SHA="$GIT_SHA" \
  docker compose -p lingopi-api "$@" down --remove-orphans

# Remove dangling images from docker images AFTER stopping containers
echo "Cleaning up dangling images..."
DANGLING_IMAGES=$(docker images -f dangling=true -q)
if [ -n "$DANGLING_IMAGES" ]; then
    docker rmi $DANGLING_IMAGES
fi

# Build docker images using Docker Compose (force rebuild)
echo "Building images..."
ENV="$ENV" APP_VERSION="$APP_VERSION" GIT_SHA="$GIT_SHA" \
  docker compose -p lingopi-api "$@" build

# Check if build was successful
if [ $? -ne 0 ]; then
    echo "Build failed! Stopping deployment."
    exit 1
fi

# Start containers using Docker Compose
echo "Starting containers..."
ENV="$ENV" APP_VERSION="$APP_VERSION" GIT_SHA="$GIT_SHA" \
  docker compose -p lingopi-api "$@" up -d

# Wait a moment for containers to start
sleep 5

# Show running containers
echo "Running containers:"
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

# Check if any containers are running
RUNNING_CONTAINERS=$(docker compose -p lingopi-api "$@" ps -q | wc -l | tr -d ' ')
if [ $RUNNING_CONTAINERS -eq 0 ]; then
    echo "Warning: No containers are running!"
    echo "Checking container logs..."
    ENV="$ENV" docker compose -p lingopi-api "$@" logs
else
    echo "Successfully deployed $RUNNING_CONTAINERS container(s)"
fi

# Final cleanup of unused Docker resources
echo "Cleaning up unused Docker resources..."
docker system prune -f
docker image prune -f -a
docker container prune -f

echo "Deployment completed!"
