# Fire Rescue

## Overview
The **Wildfire Management & Collaboration Simulator** is a strategic simulation game developed as part of the SENG 401 course (Winter 2026). Players take on the role of municipal managers tasked with protecting their communities from dynamically spreading wildfires in a fictional region.

The game challenges players to make high-stakes decisions under time pressure, manage limited resources (budgets, firefighters, equipment), and plan collaboratively with neighboring towns to prevent cascading failures. It is designed to be family-friendly and educational, teaching systems thinking and environmental awareness.

---

## 🚀 Key Features
- **Dynamic Wildfire Simulation**: Fires start and spread based on deterministic models considering wind, regional type (forest, grassland, desert), and weather modifiers.
- **Resource Management**: Allocate budgets for firefighting, infrastructure repair, and emergency preparedness. Deploy units strategically to contain outbreaks.
- **Inter-City Cooperation**: Move resources between cities to help neighbors under threat. Cooperative decisions are rewarded with reputation boosts and progression.
- **Progression & Unlocks**: Level up to unlock better tools, more complex challenges, and advanced policy options.
- **Policy Management**: Implement regional policies such as fire bans, evacuation protocols, and prescribed burns to mitigate risks.
- **Persistence Layer**: Cloud-synced game state, settings, and progression using Supabase (PostgreSQL).

---

## 🌍 SDG Alignment
This project is built with a focus on the United Nations **Sustainable Development Goals (SDGs)**:
- **SDG 13: Climate Action**: Illustrates how climate-driven disasters affect communities and emphasizes the importance of preparedness.
- **SDG 15: Life on Land**: Highlights the need to protect forests, wildlife, and ecosystems from environmental threats.
- **SDG 17: Partnerships for the Goals**: Encourages inter-city cooperation and resource sharing to manage shared crises effectively.

---

## 🛠️ Technology Stack
- **Game Engine**: [Unity 2022.3+](https://unity.com/)
- **Programming Language**: C#
- **Backend/Database**: [Supabase](https://supabase.com/) (PostgreSQL)
- **Render Pipeline**: Universal Render Pipeline (URP)
- **Version Control**: Git

---

## 📂 Project Structure
The codebase follows a clean-architecture approach:
- **`Assets/Scripts/Core`**: Singleton managers, game loops, and high-level state management.
- **`Assets/Scripts/BusinessLogic`**: Core game rules, including:
  - `Fire/`: Fire spread and intensity logic.
  - `Economy/`: Budgeting and resource allocation.
  - `ProgressionManager.cs`: Experience and level tracking.
- **`Assets/Scripts/Persistence`**:
  - `DatabaseProvider.cs`: Supabase REST API integration for cloud saving.
  - Handles players, game settings, save slots, and leaderboards.
- **`Assets/Scripts/Presentation`**: UI components, input handling, and map rendering.
- **`Database/`**: Contains `schema.sql` for setting up the Supabase database.
- **`Requirements/`**: Project documentation, SRS, and UML diagrams.


---

## 📄 License
This project is developed for educational purposes as part of the SENG 401 course at the University of Calgary.
