FROM mcr.microsoft.com/dotnet/sdk:10.0

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        mono-complete \
    && rm -rf /var/lib/apt/lists/*

ARG USERNAME=dev
ARG USER_UID=1000
ARG USER_GID=1000

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

ENV HOME=/home/dev