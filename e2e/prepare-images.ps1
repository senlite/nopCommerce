$ErrorActionPreference = 'Stop'

podman build -t e2e-web -f e2e/Dockerfile.web .
podman build -t e2e-tests -f e2e/Dockerfile.tests .
