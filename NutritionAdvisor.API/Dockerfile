FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["NutritionAdvisor.API/NutritionAdvisor.API.csproj", "NutritionAdvisor.API/"]
COPY ["NutritionAdvisor.Application/NutritionAdvisor.Application.csproj", "NutritionAdvisor.Application/"]
COPY ["NutritionAdvisor.Domain/NutritionAdvisor.Domain.csproj", "NutritionAdvisor.Domain/"]
COPY ["NutritionAdvisor.Infrastructure/NutritionAdvisor.Infrastructure.csproj", "NutritionAdvisor.Infrastructure/"]
RUN dotnet restore "NutritionAdvisor.API/NutritionAdvisor.API.csproj"
COPY . .
WORKDIR "/src/NutritionAdvisor.API"
RUN dotnet publish "NutritionAdvisor.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NutritionAdvisor.API.dll"]
