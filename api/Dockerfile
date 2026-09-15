FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files first so `dotnet restore` is cached across builds unless dependencies change.
COPY MealPenalty.Domain/*.csproj MealPenalty.Domain/
COPY MealPenalty.Application/*.csproj MealPenalty.Application/
COPY MealPenalty.Infrastructure/*.csproj MealPenalty.Infrastructure/
COPY MealPenalty.Api/*.csproj MealPenalty.Api/
RUN dotnet restore MealPenalty.Api/MealPenalty.Api.csproj

COPY MealPenalty.Domain/ MealPenalty.Domain/
COPY MealPenalty.Application/ MealPenalty.Application/
COPY MealPenalty.Infrastructure/ MealPenalty.Infrastructure/
COPY MealPenalty.Api/ MealPenalty.Api/

RUN dotnet publish MealPenalty.Api/MealPenalty.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
# Render (and similar hosts) inject $PORT at runtime; Program.cs reads it and binds Kestrel there.
EXPOSE 8080

ENTRYPOINT ["dotnet", "MealPenalty.Api.dll"]
