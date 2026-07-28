# Imagen de la API (ASP.NET Core). El restore consulta el feed NimitaCo para Es.Nimita.* (ver
# nuget.config); la credencial entra como secreto de BuildKit (id nimitaco_nuget, formato
# "Username=<usuario>;Password=<PAT>"). NUNCA por ARG/--build-arg: quedaría grabada en las capas.
# Sin secreto (o con Password vacío) el feed queda anonimo y el build sigue funcionando si hay caché.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN --mount=type=secret,id=nimitaco_nuget sh -c 'if [ -f /run/secrets/nimitaco_nuget ] && grep -q "Password=.." /run/secrets/nimitaco_nuget; then export NuGetPackageSourceCredentials_nimitaco="$(cat /run/secrets/nimitaco_nuget)"; fi && dotnet publish src/Wardkitten.Api/Wardkitten.Api.csproj -c Release -o /app'

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV TZ=Europe/Madrid
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone
WORKDIR /app
COPY --from=build /app ./
EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "Wardkitten.Api.dll"]
