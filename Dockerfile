FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Nex.Api.sln .
COPY Nex.Api/Nex.Api.csproj Nex.Api/
RUN dotnet restore

COPY . .
RUN dotnet publish Nex.Api/Nex.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV PORT=8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Nex.Api.dll"]
