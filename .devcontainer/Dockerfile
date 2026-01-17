# Microsoft provides _build_ SDKs that build fine in Linux
# so now I need that for running on Bazzite
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
COPY . .
WORKDIR /src/Necrofancy.PrepareProcedurally
RUN dotnet restore
RUN dotnet build -c Release -f net48 -o /app/build