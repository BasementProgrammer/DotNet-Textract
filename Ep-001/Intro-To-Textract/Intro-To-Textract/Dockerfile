#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Intro-To-Textract/Intro-To-Textract.csproj", "Intro-To-Textract/"]
RUN dotnet restore "Intro-To-Textract/Intro-To-Textract.csproj"
COPY . .
WORKDIR "/src/Intro-To-Textract"
RUN dotnet build "Intro-To-Textract.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Intro-To-Textract.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Intro-To-Textract.dll"]