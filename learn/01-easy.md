# Level 1 — Easy: The Big Picture

> Read this first. It explains *what* the project is in plain language, no jargon.
> If an interviewer asks "tell me about this project," your answer comes from here.

---

## The one-sentence pitch

> "It's a **notification platform** built as **microservices**. When a user registers or
> resets their password in one service, a **second service automatically sends them an email**
> — and the two services never talk to each other directly. They communicate through a
> **message broker**, so they stay completely independent."

---

## What problem does it solve?

Imagine a real app (a shop, a bank, a social app). Lots of things happen — people sign up,
reset passwords, update profiles. Each of those should trigger an **email/notification**.

If you put *everything* in one giant program (a "monolith"), the sign-up code and the
email code are tangled together. If the email system breaks, sign-up breaks too.

This project splits the work into **3 small, independent services**:

| Service | Its only job |
|---|---|
| **UserService** | Handle accounts: register, login, passwords. |
| **NotificationService** | Send & record notifications (emails). |
| **API Gateway** | The single front door — routes incoming requests to the right service. |

Each service is its own program, with its **own database**. They can be developed, deployed,
and scaled separately.

---

## The story of one click (the flow you must memorize)

1. A user clicks **"Register"** in the app.
2. The request hits the **API Gateway** (the front door).
3. The Gateway forwards it to the **UserService**.
4. UserService creates the account and shouts out a message: *"Hey, a user just registered!"*
5. That message goes into **RabbitMQ** (a message broker — like a post office for messages).
6. The **NotificationService** is subscribed to those messages. It picks it up and **sends a
   welcome email** (in this project, the email is *faked* — it just prints to the log).
7. NotificationService **saves a record** of that email in its own database.

The magic: **step 4 doesn't wait for step 6.** UserService fires the message and immediately
moves on. This is called **asynchronous** ("fire-and-forget") communication. The user gets a
fast response; the email happens in the background.

```
You ──register──▶ Gateway ──▶ UserService ──"user registered!"──▶ RabbitMQ ──▶ NotificationService ──▶ 📧 email
```

---

## Two ways the services communicate

This is a favorite interview point — there are **two kinds** of communication here:

1. **Synchronous (HTTP):** You → Gateway → UserService. You ask, you wait, you get an answer.
   Like a phone call.
2. **Asynchronous (messages via RabbitMQ):** UserService → NotificationService. One service
   drops a message and walks away; the other picks it up whenever it's ready. Like sending
   a letter — you don't wait by the mailbox.

---

## The key rule: each service owns its data

- UserService has `userservice.db` (users, passwords).
- NotificationService has `notificationservice.db` (the notification history).
- **Neither service is allowed to touch the other's database.**

Why? So they're truly independent. If you want user info inside NotificationService, you don't
reach into the user database — you get it from the **message** that was sent. This is the single
most important rule in microservices.

---

## The tech, in one line each

- **ASP.NET Core (.NET 10)** — the framework all three services are written in (C#).
- **RabbitMQ** — the "post office" that carries messages between services.
- **MassTransit** — a helper library that makes sending/receiving RabbitMQ messages easy.
- **Ocelot** — the library that powers the API Gateway (the front door / router).
- **Entity Framework Core** — talks to the databases so we write C# instead of raw SQL.
- **JWT** — a secure "ID badge" token the user gets after login, to prove who they are.
- **Docker** — packages everything so the whole system starts with one command.

---

## What you should be able to say out loud

- "There are 3 services: Users, Notifications, and a Gateway."
- "They don't share a database — each owns its own."
- "They talk two ways: HTTP through the gateway, and async messages through RabbitMQ."
- "When a user registers, UserService publishes an event, and NotificationService reacts to it
  by sending a welcome email."
- "The email is a stub — it logs instead of really sending — so the integration point is clean."

➡️ **Next:** `02-medium.md` explains *how* each of these actually works under the hood.
