# Base image for senlite/nopCommerce cloud agents.
# System packages and rclone are installed here; repository bootstrap runs in install.sh.
FROM ubuntu:24.04

ENV DEBIAN_FRONTEND=noninteractive

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        bash \
        ca-certificates \
        curl \
        git \
        gnupg \
        jq \
        unzip \
        wget \
    && rm -rf /var/lib/apt/lists/* \
    && curl -fsSL https://rclone.org/install.sh | bash

# nopCommerce / Check Engine cloud agents run as ubuntu when user is set in environment.json.
RUN useradd -m -s /bin/bash ubuntu || true

USER ubuntu
WORKDIR /home/ubuntu
