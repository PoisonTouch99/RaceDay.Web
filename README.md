# RaceDay Platform

A race event management platform built with ASP.NET Core. Organisers create and manage events; Participants browse and enter races.

---

## Running Locally



---


Containers `event-banners` and `profile-pictures` are created automatically on first upload.

---

## Docker

### Build the MVC image

```bash
cd RaceDay.Web
docker build -t raceday-web .
```



Then open: `http://localhost:8080`

> **Note:** Replace `host.docker.internal` with your machine's local IP if running on Linux.

### Stop & remove the container

```bash
docker stop raceday-web && docker rm raceday-web
```

---

## Seeded Test Accounts

All passwords: `Passw0rd!`

| Role        | Email                            |
|-------------|----------------------------------|
| Organiser   | sarah.nkosi@raceday.co.za        |
| Organiser   | david.botha@raceday.co.za        |
| Participant | thabo.mokoena@example.com        |
| Participant | emma.vanwyk@example.com          |
| Participant | kagiso.sithole@example.com       |

---

## CI/CD

GitHub Actions workflow (`.github/workflows/build.yml`) builds both the API and MVC projects on every push.

---

## YouTube Demo

[Link to be added before submission]
