# 🧠 NeuroPath - Adaptive Cognitive Rehabilitation Platform

<div align="center">

![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Blazor Server](https://img.shields.io/badge/Blazor-Server-512BD4?style=for-the-badge&logo=blazor&logoColor=white)
![LM Studio](https://img.shields.io/badge/LM%20Studio-Compatible-00A4EF?style=for-the-badge&logo=ai&logoColor=white)


**AI-Powered Cognitive Training with Real-Time Personalization**

*Personalized brain training powered by locally-hosted AI. Adapt to your unique rehabilitation journey with real-time difficulty adjustment.*

[Features](#-features) • [Tech Stack](#-tech-stack) • [AI Integration](#-ai-integration) • [Installation](#-installation) • [Team](#-our-team)

</div>

---

## Overview

**NeuroPath** is an innovative AI-powered web application designed for personalized cognitive rehabilitation and training. The platform helps individuals recovering from traumatic brain injuries (TBI), stroke, neurodegenerative disorders, or age-related cognitive decline through scientifically-grounded cognitive games and wellness activities.

### Key Highlights

- **AI-Powered Personalization** - Real-time difficulty adjustment based on user performance
- **Privacy First** - All AI processing happens locally; no data sent to external servers
- **7 Cognitive Games** - Targeting memory, attention, processing speed, and executive function
- **9 Wellness Activities** - Holistic mental health support including breathing exercises and journaling
- **Comprehensive Analytics** - Track progress with detailed performance metrics
- **Therapist Dashboard** - Healthcare professionals can monitor and manage patient progress

---

## Features

### Cognitive Games

| Game | Cognitive Domain | Scientific Basis |
|------|-----------------|------------------|
| **Memory Match** | Working Memory | Baddeley's Working Memory Model |
| **Reaction Trainer** | Processing Speed | Information Processing Theory |
| **Sorting Task** | Executive Function | Miyake's Executive Function Framework |
| **Pattern Copy** | Visual-Spatial Memory | Dual Coding Theory |
| **Trail Making** | Cognitive Flexibility | Shifting/Task-Switching Paradigm |
| **Stroop Test** | Selective Attention | Stroop Effect (1935) |
| **Dual Task** | Divided Attention | Resource Allocation Theory |

### Wellness Activities

| Activity | Purpose |
|----------|---------|
| **Breathing Exercise** | Parasympathetic activation & stress relief |
| **Daily Journal** | Emotional processing & reflection |
| **Word Association** | Semantic network activation |
| **Story Recall** | Episodic memory consolidation |
| **Mental Math** | Numerical cognition training |
| **Focus Tracker** | Sustained attention development |
| **Word Puzzles** | Linguistic processing enhancement |
| **Number Sequence** | Pattern recognition training |
| **Sound Therapy** | Auditory relaxation & mindfulness |

### User Roles

| Role | Capabilities |
|------|-------------|
| **User** | Play cognitive games & activities, track personal progress, view statistics, share progress with family members |
| **Therapist** | Manage patients, monitor progress, assign treatments, view comprehensive analytics |

---

## Tech Stack

### Backend
- **.NET 10** - Latest .NET framework
- **Blazor Server** - Interactive Server Components for real-time UI
- **Entity Framework Core 10** - ORM for database operations
- **JWT Authentication** - Secure token-based authentication with BCrypt password hashing

### Frontend
- **Blazor Components** - Component-based UI architecture
- **Bootstrap Icons** - Modern iconography
- **Custom CSS** - Responsive design

### Data Storage
- **JSON-based storage** - Lightweight, efficient session management (development)
- **SQL Server** - Production-ready relational database support

### AI/ML
- **LM Studio** (Current) - Local AI model hosting
- **Microsoft Phi-4** - Small Language Model for personalized feedback

---

## AI Integration

### Current Setup: LM Studio with Phi-4

The platform currently uses **LM Studio** to host the **Microsoft Phi-4** language model locally. This provides:

- **Privacy** - No data leaves your machine
- **No API Costs** - Free local inference
- **Fast Response** - 8-15 second feedback generation
- **Offline Capable** - Works without internet connection

### Flexible AI Backend

> **Important:** While we currently use LM Studio with Phi-4, the platform is designed to work with **any compatible SLM (Small Language Model) or LLM (Large Language Model)**!

You can use:

| Platform | Models | Configuration |
|----------|--------|---------------|
| **LM Studio** | Phi-4, Phi-4-mini, Llama 3, Mistral, etc. | Default: `localhost:1234` |
| **Ollama** | Phi-4, Llama 3.2, Mistral, CodeLlama, etc. | Configure: `localhost:11434` |
| **vLLM** | Any HuggingFace model | Configure custom endpoint |
| **LocalAI** | Multiple model formats | Configure custom endpoint |
| **text-generation-webui** | Any GGUF/GPTQ model | Configure custom endpoint |

### Changing the AI Backend

To use a different AI provider, modify the endpoint in `AIAnalysisEngine.cs`:

```csharp
// Default LM Studio configuration
private const string LM_STUDIO_ENDPOINT = "http://localhost:1234/v1/chat/completions";

// For Ollama, change to:
// private const string LM_STUDIO_ENDPOINT = "http://localhost:11434/v1/chat/completions";

// For custom endpoint:
// private const string LM_STUDIO_ENDPOINT = "http://your-server:port/v1/chat/completions";
```

### Recommended Models

| Model | Size | Best For |
|-------|------|----------|
| **Phi-4** | ~2.7B | Balanced performance & quality (Recommended) |
| **Phi-4-mini** | ~1.3B | Faster inference, lower resource usage |
| **Llama 3.2 3B** | ~3B | High-quality responses |
| **Mistral 7B** | ~7B | Maximum quality (requires more RAM) |

---

## Installation

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [LM Studio](https://lmstudio.ai/) or compatible AI backend
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)

### Quick Start

1. **Clone the repository**
   ```bash
   git clone https://github.com/your-username/neuropath-cognitive-rehabilitation.git
   cd neuropath-cognitive-rehabilitation
   ```

2. **Install dependencies**
   ```bash
   dotnet restore
   ```

3. **Set up LM Studio**
   - Download and install [LM Studio](https://lmstudio.ai/)
   - Download the **Phi-4** or **Phi-4-mini** model
   - Start the local server on port `1234`

4. **Run the application**
   ```bash
   dotnet run
   ```

5. **Open in browser**
   ```
   https://localhost:5001
   ```

### Configuration

Update `appsettings.json` for your environment:

```json
{
  "Jwt": {
    "Secret": "your-super-secret-jwt-key-change-this-in-production",
    "Issuer": "NeuroPathApp",
    "Audience": "NeuroPathAppUsers",
    "ExpirationMinutes": 60
  },
  "AppSettings": {
    "AppName": "Cognitive Rehabilitation Platform",
    "MaxGameDifficulty": 10,
    "MinGameDifficulty": 1
  }
}
```

---


## Our Team

<div align="center">

### Development Team

I'm very grateful to acknowledge the dedicated team members who contributed to the development of this platform:

1. Rishika Ponnalagappan (https://github.com/Rishika057)
2. Bhuvanan P (https://github.com/bhuvan0003)
3. Mariyam Jasmine S A (https://github.com/mariyam-jasmine)

### Supervisors & Mentors

Special thanks to my mentor for his invaluable guidance:

- **Dr. M. Rajasekaran / Associate Professor** - Academic Supervisor


</div>

---

## Acknowledgments

- **Microsoft** - For .NET 10, Blazor, and the Phi-4 language model
- **LM Studio** - For providing an excellent local AI hosting solution
- **The Open Source Community** - For the libraries and tools that made this possible
- **Healthcare Professionals** - For domain expertise and validation


---

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

---

## Contact

For questions, feedback, or support, please open an issue on GitHub.

---

<div align="center">

**Made with ❤️ for Cognitive Health**

*Empowering rehabilitation through intelligent technology*

</div>
