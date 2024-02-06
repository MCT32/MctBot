FROM mcr.microsoft.com/dotnet/sdk:8.0

WORKDIR /src
COPY . .

RUN dotnet build -c Release -o /app
RUN dotnet publish -o /publish

WORKDIR /publish
CMD ["./MctBot"]