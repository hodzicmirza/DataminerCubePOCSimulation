FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["LinuxServerDataminerPOC.csproj", "./"]
RUN dotnet restore "LinuxServerDataminerPOC.csproj"
COPY . .
RUN dotnet publish "LinuxServerDataminerPOC.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:5051
EXPOSE 5051
ENTRYPOINT ["dotnet", "LinuxServerDataminerPOC.dll"]
