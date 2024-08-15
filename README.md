<div align="center">

  <div>
    <img src="https://img.shields.io/badge/Next.js-%23000000.svg?style=for-the-badge&logo=nextdotjs&logoColor=white" alt="Next.js" />
    <img src="https://img.shields.io/badge/-Tailwind_CSS-black?style=for-the-badge&logoColor=white&logo=tailwindcss&color=06B6D4" alt="tailwindcss" />
    <img src="https://img.shields.io/badge/-Typescript-black?style=for-the-badge&logoColor=white&logo=typescript&color=3178C6" alt="typescript" />
    <img src="https://img.shields.io/badge/Redis-%23DC382D.svg?style=for-the-badge&logo=redis&logoColor=white" alt="redis" />
    <img src="https://img.shields.io/badge/PostgreSQL-%23336791.svg?style=for-the-badge&logo=postgresql&logoColor=white" alt="postgresql" />
    <img src="https://img.shields.io/badge/C%23-%23239120.svg?style=for-the-badge&logo=c-sharp&logoColor=white" alt="C#" />
    <img src="https://img.shields.io/badge/Serilog-%230D5D90.svg?style=for-the-badge&logo=serilog&logoColor=white" alt="Serilog" />
    <img src="https://img.shields.io/badge/Docker-%230db7ed.svg?style=for-the-badge&logo=docker&logoColor=white" alt="Docker" />
  </div>

  <h3 align="center">Airbnb Clone</h3>
  
</div>

## 📋 <a name="table">Table of Contents</a>

1. ⚙️ [Tech Stack](#tech-stack)
2. 🔋 [Features](#features)
3. 🤸 [Quick Start](#quick-start)

## <a name="tech-stack">⚙️ Tech Stack</a>

- ASP.NET Core
- Next.JS
- C#
- Typescript
- TailwindCSS
- Redis
- RabbitMQ
- Serilog
- Docker
- ShadCN UI
- MediatR
- Tanstack React query

## <a name="features">🔋 Features</a>

👉 Authentication System: A self-made robust authentication with JWT token based on a secret key.

👉 Explore: Explore many apartments that might catch your fancy!

👉 React Query: Incorporating Tanstack react query for mutations.

👉 Horizontally scaling with a Yarp Balancer.

👉 Logging: Implement logging with Serilog for monitoring.


## <a name="quick-start">🤸 Quick Start</a>

Follow these steps to set up the project locally on your machine.

**Prerequisites**

Make sure you have the following installed on your machine:

- [Git](https://git-scm.com/)
- [Node.js](https://nodejs.org/en)
- [Bun](https://bun.sh)
- [npm](https://www.npmjs.com/) (Node Package Manager)
- [Visual Studio 2022](https://visualstudio.microsoft.com/vs/)
- [Docker](https://www.docker.com/)

**Cloning the Repository**

```bash
git clone https://github.com/HilthonTT/Lodge.git
```

**_Frontend_**

Navigate to the frontend directory:

```bash
cd src/frontend
```

Install the project dependencies using npm:

```bash
bun install
```

**Set Up Environment Variables**

Create a new file named `.env` in the root of your project and add the following content:

```env
NEXT_PUBLIC_API_BASE_URL="https://localhost:5001"
NEXT_PUBLIC_API_VERSION="v1"
```

**Running the Project**

```bash
bun run dev
```

Open [http://localhost:3000](http://localhost:3000) in your browser to view the project.

**_Backend_**

Navigate to the backend directory:

```bash
cd src/backend
```

Setup your appsettings.json (normally already prefilled) but you must fill in your stripe credentials here:
Open stripe [Stripe](https://dashboard.stripe.com/)

```appsetings.json
"Stripe": {
    "ReturnUrl": "http://localhost:3000/bookings",
    "SecretKey": "SECRET-KEY",
    "WebhookSecret": "WEBHOOK-SECRET"
  }
```

Running the docker yml file:

```bash
docker-compose up
```

This command will start the necessary services as defined in your Docker Compose configuration.

If everything succeeded, there should be an API at https://localhost:5001/swagger.json and a Yarp Balancer at https://localhost:8081/swagger.json

Enjoy!
