FROM mcr.microsoft.com/dotnet/sdk:10.0

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        mono-complete \
    && rm -rf /var/lib/apt/lists/*

ARG USERNAME=dev
ARG USER_UID=1000
ARG USER_GID=1000

# Create dev user and group, handling pre-existing UIDs/GIDs in the base image gracefully.
# Also pre-creates .nuget with correct ownership once the home directory is guaranteed available.
RUN set -eux; \
    existing_group="$(getent group "${USER_GID}" | cut -d: -f1 || true)"; \
    if [ -z "${existing_group}" ]; then \
        groupadd --gid "${USER_GID}" "${USERNAME}"; \
    elif [ "${existing_group}" != "${USERNAME}" ]; then \
        groupmod -n "${USERNAME}" "${existing_group}"; \
    fi; \
    existing_user="$(getent passwd "${USER_UID}" | cut -d: -f1 || true)"; \
    if [ -z "${existing_user}" ]; then \
        useradd --uid "${USER_UID}" --gid "${USER_GID}" --create-home --shell /bin/bash "${USERNAME}"; \
    elif [ "${existing_user}" != "${USERNAME}" ]; then \
        usermod -l "${USERNAME}" "${existing_user}"; \
        usermod -d "/home/${USERNAME}" -m "${USERNAME}"; \
        usermod -g "${USER_GID}" "${USERNAME}"; \
    fi; \
    mkdir -p "/home/${USERNAME}/.nuget/NuGet"; \
    chown -R "${USER_UID}:${USER_GID}" "/home/${USERNAME}/.nuget"; \
    chmod 700 "/home/${USERNAME}"; \
    chmod -R u+rwX,go-rwx "/home/${USERNAME}/.nuget"

# Pre-create writable directories with correct ownership.
# Required because the container runs read-only; named volumes mount over these paths
# but inherit ownership from the image layer since Rider strips non-standard mount options (:U, :z).
RUN mkdir -p \
        "/home/${USERNAME}/.dotnet" \
        "/home/${USERNAME}/.config/JetBrains" \
        "/home/${USERNAME}/.cache/JetBrains" \
        "/home/${USERNAME}/.local/share/JetBrains" \
        "/home/${USERNAME}/.claude" \
    && touch "/home/${USERNAME}/.gitconfig" \
    && chown -R "${USER_UID}:${USER_GID}" \
        "/home/${USERNAME}/.dotnet" \
        "/home/${USERNAME}/.config/JetBrains" \
        "/home/${USERNAME}/.cache/JetBrains" \
        "/home/${USERNAME}/.local/share/JetBrains" \
        "/home/${USERNAME}/.claude" \
        "/home/${USERNAME}/.gitconfig" \
    && chmod -R u+rwX,go-rwx \
        "/home/${USERNAME}/.dotnet" \
        "/home/${USERNAME}/.config/JetBrains" \
        "/home/${USERNAME}/.cache/JetBrains" \
        "/home/${USERNAME}/.local/share/JetBrains" \
        "/home/${USERNAME}/.claude"

ENV HOME=/home/dev
