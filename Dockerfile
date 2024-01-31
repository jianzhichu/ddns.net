#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 3344

COPY . .
ENV ASPNETCORE_URLS http://*:3344
ENTRYPOINT ["dotnet", "ddns.net.dll"]
ENV TZ=Asia/Shanghai