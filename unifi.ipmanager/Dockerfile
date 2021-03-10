#Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
#For more information, please see https://aka.ms/containercompat

FROM mcr.microsoft.com/dotnet/core/aspnet:5.0-focal AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0-focal AS build
WORKDIR /src
COPY ["unifi.ipmanager.csproj", "unifi.ipmanager.csproj"]
RUN dotnet restore "unifi.ipmanager.csproj"
COPY . .
RUN dotnet build "unifi.ipmanager.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "unifi.ipmanager.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "unifi.ipmanager.dll"]