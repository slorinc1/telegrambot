FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS base
WORKDIR /app
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS http://*:80
EXPOSE 80
EXPOSE 443

FROM base AS builder
ARG Configuration=Release
WORKDIR /src
COPY *.sln ./
COPY TelegramBotHost/TelegramBotHost.csproj TelegramBotHost/
RUN dotnet restore
COPY . .
WORKDIR /src/TelegramBotHost
RUN dotnet build -c $Configuration -o /app

FROM builder AS publish
ARG Configuration=Release
RUN dotnet publish -c $Configuration -o /app

#FROM base AS final
#WORKDIR /app
#COPY --from=publish /app .
#ENTRYPOINT ["dotnet", "TelegramBotHost.dll"]

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app
COPY --from=publish /app/ .
CMD ASPNETCORE_URLS=http://*:$PORT dotnet TelegramBotHost.dll
###