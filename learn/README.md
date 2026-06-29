# 📚 Learn This Project — Interview Prep

Three guides that explain this codebase at increasing depth. Read them in order; each builds
on the last. By the end you should be able to explain the project, defend its design, and answer
follow-up questions.

| File | Level | What it covers | Read when |
|---|---|---|---|
| [`01-easy.md`](01-easy.md) | 🟢 Easy | Plain-language overview: what it is, the register→email flow, the tech in one line each. | First. The "tell me about your project" answer. |
| [`02-medium.md`](02-medium.md) | 🟡 Medium | How it works: clean architecture layers, CQRS, the event flow with class names, JWT, EF Core, Docker. | After easy. Most interview questions live here. |
| [`03-hard.md`](03-hard.md) | 🔴 Hard | Trade-offs, distributed-systems concepts (eventual consistency, pub/sub, outbox), security details, production gaps, and a rapid-fire Q&A drill. | Last. To sound senior and defend decisions. |

---

## How to use this for an interview

1. **Read all three once**, top to bottom.
2. **Memorize two things from `01-easy.md`:** the one-sentence pitch and the register→email flow.
3. **Practice drawing** the whiteboard diagram at the end of `03-hard.md` from memory.
4. **Drill the Q&A** in section 7 of `03-hard.md` out loud until the answers are natural.
5. If you only have 5 minutes before the call: re-read `01-easy.md` + the Q&A drill.

## The flow you must never fumble

> Client → **Gateway** (routes + validates JWT) → **UserService** creates the user and
> **publishes `UserRegisteredEvent`** → **RabbitMQ** → **NotificationService** consumes it →
> renders the Welcome template → **email stub logs it** → saves the notification record.
> UserService never waits for any of that — it's **asynchronous** and **eventually consistent**.
