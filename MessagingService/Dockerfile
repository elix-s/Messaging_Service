FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["MessagingService.csproj", "./"]
RUN dotnet restore "MessagingService.csproj"

COPY . .
RUN dotnet publish "MessagingService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 80
ENTRYPOINT ["dotnet", "MessagingService.dll", "--urls", "http://0.0.0.0:80"]

