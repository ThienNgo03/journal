FROM mcr.microsoft.com/dotnet/sdk:9.0 as build
WORKDIR /app
EXPOSE 80

COPY journal.sln journal.sln
COPY src/Journal/Journal.csproj src/Journal/Journal.csproj

RUN dotnet restore journal.sln

COPY src/Journal src/Journal
WORKDIR /app/src/Journal
RUN dotnet publish -c Release -o /app/src/Journal/out
COPY src/Journal/Exercises/Exercises.xlsx /app/src/Journal/out/Exercises/Exercises.xlsx
COPY src/Journal/Muscles/Muscles.xlsx /app/src/Journal/out/Muscles/Muscles.xlsx
COPY src/Journal/ExerciseMuscles/ExerciseMuscles.xlsx /app/src/Journal/out/ExerciseMuscles/ExerciseMuscles.xlsx

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

COPY --from=build /app/src/Journal/out .
ENTRYPOINT [ "dotnet", "Journal.dll" ]