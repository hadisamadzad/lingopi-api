#!/bin/sh

set -eu

# Read the environment from either the original positional form or the
# explicit --ENV form used by local deployment commands.
if [ "$#" -eq 1 ] && [ -n "$1" ] && [ "$1" != "--ENV" ]; then
    ENV=$1
elif [ "$#" -eq 2 ] && [ "$1" = "--ENV" ] && [ -n "$2" ]; then
    ENV=$2
else
    echo "Usage: $0 <environment> | $0 --ENV <environment>" >&2
    exit 1
fi

APP_VERSION=${APP_VERSION:-unknown}
GIT_SHA=${GIT_SHA:-unknown}
DOCKER_BUILDKIT=${DOCKER_BUILDKIT:-0}
COMPOSE_DOCKER_CLI_BUILD=${COMPOSE_DOCKER_CLI_BUILD:-0}
export DOCKER_BUILDKIT COMPOSE_DOCKER_CLI_BUILD

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

wait_for_endpoint() {
    endpoint=$1
    attempts=30

    while [ "$attempts" -gt 0 ]; do
        if curl --fail --silent "$endpoint" >/dev/null 2>&1; then
            return 0
        fi

        attempts=$((attempts - 1))
        sleep 2
    done

    echo "Service health check failed: $endpoint" >&2
    ENV="$ENV" docker compose -p lingopi-api "$@" logs --tail=100 gateway identity lingo >&2
    exit 1
}

echo "Waiting for services to become healthy..."
wait_for_endpoint "http://localhost:45000/api/identity/health"
wait_for_endpoint "http://localhost:45000/api/lingo/health"

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
