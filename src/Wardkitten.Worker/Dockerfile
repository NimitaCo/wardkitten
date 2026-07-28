# Imagen del worker de evaluación (sin servidor HTTP). El restore consulta el feed NimitaCo para
# Es.Nimita.* (ver nuget.config); la credencial entra como secreto de BuildKit (id nimitaco_nuget,
# formato "Username=<usuario>;Password=<PAT>"). NUNCA por ARG/--build-arg: quedaría en las capas.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN --mount=type=secret,id=nimitaco_nuget sh -c 'if [ -f /run/secrets/nimitaco_nuget ] && grep -q "Password=.." /run/secrets/nimitaco_nuget; then export NuGetPackageSourceCredentials_nimitaco="$(cat /run/secrets/nimitaco_nuget)"; fi && dotnet publish src/Wardkitten.Worker/Wardkitten.Worker.csproj -c Release -o /app'

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV TZ=Europe/Madrid
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "Wardkitten.Worker.dll"]
