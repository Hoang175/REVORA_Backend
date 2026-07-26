# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Sửa đường dẫn để Docker tìm thấy file .csproj nằm trong thư mục con REVORA_BE
COPY ["REVORA_BE/REVORA_BE.csproj", "REVORA_BE/"]
RUN dotnet restore "REVORA_BE/REVORA_BE.csproj"

# Copy toàn bộ source code vào container
COPY . .

# Di chuyển vào đúng thư mục con chứa code để tiến hành Build
WORKDIR "/src/REVORA_BE"
RUN dotnet build "REVORA_BE.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "REVORA_BE.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# ÉP TOÀN BỘ CONTAINER BẬT CHẾ ĐỘ THỜI GIAN LEGACY CHẤP NHẬN MỌI ĐỊNH DẠNG DATETIME CỦA NHÓM
ENV DOTNET_Npgsql_EnableLegacyTimestampBehavior=true

ENTRYPOINT ["dotnet", "REVORA_BE.dll"]