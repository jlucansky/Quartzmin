FROM microsoft/dotnet:2.1-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY ["Source/Examples/AspNetCoreDocker/AspNetCoreDocker.csproj", "Source/Examples/AspNetCoreDocker/"]
RUN dotnet restore "Source/Examples/AspNetCoreDocker/AspNetCoreDocker.csproj"
COPY . .
WORKDIR "/src/Source/Examples/AspNetCoreDocker"
RUN dotnet build "AspNetCoreDocker.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "AspNetCoreDocker.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "AspNetCoreDocker.dll"]