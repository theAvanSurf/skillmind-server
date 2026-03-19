# Use the official .NET SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files and restore as distinct layers
COPY ["SkillMind.WebAPI/SkillMind.WebAPI.csproj", "SkillMind.WebAPI/"]
COPY ["SkillMind.Infrastructure.Identity/SkillMind.Infrastructure.Identity.csproj", "SkillMind.Infrastructure.Identity/"]
COPY ["SkillMind.Infrastructure.Persistence/SkillMind.Infrastructure.Persistence.csproj", "SkillMind.Infrastructure.Persistence/"]
COPY ["SkillMind.Core.Application/SkillMind.Core.Application.csproj", "SkillMind.Core.Application/"]
COPY ["SkillMind.Core.Domain/SkillMind.Core.Domain.csproj", "SkillMind.Core.Domain/"]

RUN dotnet restore "SkillMind.WebAPI/SkillMind.WebAPI.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/SkillMind.WebAPI"
RUN dotnet build "SkillMind.WebAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SkillMind.WebAPI.csproj" -c Release -o /app/publish

# Development stage with SDK and tools
FROM build AS development
WORKDIR /src/SkillMind.WebAPI
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"
CMD ["dotnet", "run", "--no-launch-profile"]

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SkillMind.WebAPI.dll"]
