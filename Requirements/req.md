# SENG 401
## Software Requirements Specification (SRS)
**Project:** Wildfire Management Game
**Name:** [Undecided]

**Term:** Winter 2026  
**Section:** L02  
**Group:** 08  
**Date:** 2026-Feb-20

### Group Members 

| Name | UCID |
| :--- | :--- |
| Tanzim Taseen | 30227481 |
| Mika Bobo | 30123273 |
| Natinael Nega | 30228957 |
| Siju Xavier | 30195663 |
| Siddhartha Paudel | 30172632 |
| Kareem Hussein | 30187400 |

---

## 1. Introduction
People often take the environment for granted, and human activity can increase the risk of disasters such as wildfires, even when they are not directly caused by humans. Wildfire Management and Collaboration Simulator is a strategy and simulation game set in a fictional region with multiple cities and towns. Players take on the role of municipal managers tasked with protecting their communities from dynamically spreading wildfires. The game challenges players to make high-stakes decisions under time pressure, manage resources, and plan collaboratively, illustrating how cooperation improves outcomes during shared crises. The simulator is family-friendly and educational, teaching systems thinking and environmental awareness without being preachy.

### 1.1 Definitions and Acronyms
- **SDG (Sustainable Development Goals):** A set of 17 global goals established by the United Nations to address social, economic, and environmental challenges, guiding sustainable development efforts worldwide.
- **Shared State:** The consistent, synchronized game data maintained by backend systems to ensure all players in multiplayer mode observe identical world conditions, resource states, and event outcomes in real time.
- **Cascading Failures:** A chain reaction of negative consequences that occurs when delayed, poorly planned, or selfish decisions in one city or region propagate across the system, affecting multiple areas and compounding overall damage.
- **Deterministic Simulation:** A simulation model in which identical initial conditions and player inputs always produce identical outcomes, ensuring predictable and reproducible wildfire spread and disaster behavior across supported platforms.

### 1.2 Scope
The project focuses on developing an interactive strategy and simulation game that demonstrates the interconnectedness of climate-related disasters, resource management, and cooperative decision-making. The game addresses the following Sustainable Development Goals:
- **SDG 13: Climate Action** - Illustrates how climate-driven disasters affect communities and emphasizes the importance of prevention and preparedness.
- **SDG 15: Life on Land** - Highlights the need to protect forests, wildlife, and ecosystems from environmental threats.
- **SDG 17: Partnerships for the Goals** - Encourages inter-city cooperation, resource sharing, and mutual support to manage disasters effectively.

The project’s primary deliverable is the single-player game experience, while multiplayer functionality is considered an optional enhancement to be implemented if time and resources allow. The scope includes:
- Dynamic wildfire simulation across multiple cities and regions.
- Resource allocation, emergency response, and policy management systems.
- Progression and scaling of disaster difficulty and complexity.
- Cooperative decision-making mechanics (optional multiplayer).

The project does not include:
- Real-world integration with actual fire data or emergency services.
- Virtual reality or augmented reality interfaces.
- Complex economic simulations beyond simplified city budgets, policy and resource management.

### 1.3 Intended Audience
The simulator targets teenagers and adults, including students and casual players who enjoy strategy and simulation games. It is designed to be family-friendly, accessible, and educational while maintaining engaging gameplay. Players will gain:
- Systems thinking skills through managing interconnected city and regional systems.
- Awareness of environmental risks and the consequences of human actions.
- Understand the value of cooperation and resource sharing in crisis scenarios.

The game is for desktop platforms (Windows and macOS) with WebGL as a possible extension. Multiplayer gameplay is optional and primarily aimed at providing enhanced collaborative learning experiences.

---

## 2. Project Description

### 2.1 Product Perspective
The software is developed using the Unity engine and programmed in C#. It is designed as a cross-platform application for Windows, macOS, and potentially WebGL. The system is primarily a standalone single-player experience but can transition to a connected mode, leveraging backend services linked to a PostgreSQL database to support multiplayer synchronization and shared state. This architecture allows consistent simulation results and coordinated gameplay across multiple players when multiplayer is enabled.

### 2.2 Product Function
Players take on the role of municipal managers, responsible for budgeting, deploying emergency services, and enforcing policy decisions under time pressure. Wildfires start and spread dynamically, posing threats to people, wildlife, infrastructure, and the local economy. Players must make strategic tradeoffs between safeguarding their own city and supporting the broader region. When a city is directly affected by a disaster, other cities can respond by sending firefighters, equipment, emergency support, or funding. The game’s mechanics emphasize resource management, cooperation, and forward planning to minimize cascading failures.

### 2.3 Operating Environment and Technology Stack
The game runs on Windows and macOS, with WebGL support considered an optional deployment target. The frontend client is built in Unity using C#. The backend architecture relies on C# for the backend API and PostgreSQL for persistent storage to manage player progression, city reputation, and possibly multiplayer persistence. Custom backend services handle real-time synchronization of shared state for multiplayer sessions, ensuring consistent and reliable gameplay when collaboration is active.

### 2.4 Design and Implementation Constraints
The game is designed to progressively increase in difficulty, introducing larger fires, multiple simultaneous disasters, and tighter resource limits over time. Players must manage limited budgets, emergency services, and policy options, ensuring that strategic tradeoffs remain meaningful throughout gameplay. As the player survives and demonstrates competence, new and better tools and resources become available. Access to multiplayer features is optional and only becomes available once a player reaches a certain progression level, which allows the system to scale from a standalone single-player experience to a connected, collaborative one without disrupting the core gameplay. All mechanics are constrained by these rules to maintain a balanced and challenging, yet straightforward, experience while reinforcing strategic decision-making.

### 2.5 Assumptions and Dependencies
The system assumes a reliable connection to the PostgreSQL database to persist player progression, city reputation, and other game state data. Multiplayer functionality depends on stable backend services to synchronize shared state between players, ensuring that cooperative gameplay reflects consistent environmental conditions across cities. The wildfire spread and disaster mechanics are built on deterministic simulation models within Unity, which rely on predefined environmental parameters and player actions. It is assumed that players will interact with the game on supported platforms, Windows, macOS, or WebGL, so the system can deliver consistent performance and functionality across these environments.

---

## 3. System Features

### 3.1 Functional Requirements (FRs)
Functional requirements describe what the system must do; specific features, actions, or responses the game needs to implement. The Functional Requirements cover:

**Fire and Disaster Mechanics**
- The system shall generate wildfires dynamically on the map based on environmental and player factors.
- The system shall calculate fire spread across adjacent areas based on wind, regional type (e.g., forest, grassland, desert), and containment status.
- The system shall escalate disaster difficulty over time by increasing fire frequency, fire spread rate and overlapping events.

**Player Actions and Resource Management**
- The system shall allow players to allocate budgets for firefighting, infrastructure repair, and emergency preparedness.
- The system shall allow players to deploy firefighters, equipment, and emergency support to affected cities.
- The system shall track available resources and prevent allocation beyond current limits.

**Inter-City Cooperation**
- The system shall allow the player to move resources from one city to aid a neighboring city under threat. The ability to send aid depends on available resources and the player's progression level.
- The system shall reward cooperative decisions with reputation boosts and progression points, reflecting increased community trust.
- The system shall penalize delayed or selfish actions with cascading failures affecting multiple cities.

**Simulation and Feedback**
- The system shall calculate environmental loss based on fire spread and response efficiency.
- The system shall track player response times and adjust outcomes dynamically (faster response means lower loss).
- The system shall display real-time visual feedback of fires, resource allocation, and city status.

**Progression and Unlocks**
- The system shall implement player progression levels that unlock better tools and resources and more complex challenges.
- The system shall scale fire difficulty and resource scarcity in proportion to player progression.

**Optional Multiplayer (if implemented)**
- The system shall allow multiple players to manage designated regions within the same shared world.
- The system shall synchronize shared state across all connected clients through backend services.
- The system shall maintain consistent resource counts, wildfire status, environmental conditions, and event outcomes for all players in real time.
- The system shall allow players to provide assistance, resources, or emergency support to other regions during disaster events.

### 3.2 Requirement Traceability Matrix
| Requirement ID | Requirement Description | Design Reference | Priority |
| :--- | :--- | :--- | :--- |
| **REQ-001** | The system shall generate wildfires dynamically based on environmental conditions and player actions. | Fire Simulation Module | High |
| **REQ-002** | The system shall calculate fire spread to adjacent regions using fire intensity, regional type, and weather modifiers. | Fire Spread Engine | High |
| **REQ-003** | The system shall allow players to allocate budgets for firefighting, emergency services, and infrastructure repairs. | Resource Management Module | High |
| **REQ-004** | The system shall allow players to deploy firefighters, equipment or resources from their inventory and move resources between cities to aid affected regions. | Deployment Module | High |
| **REQ-005** | The system shall provide a family-friendly, educational UI that clearly shows fire locations, resource levels, and city status. | UI Module | High |
| **REQ-006** | The system shall track player response times and adjust environmental loss accordingly. | Fire Simulation / Scoring System | Medium |
| **REQ-017** | The system shall simulate cascading failures triggered by delayed, poorly planned, or selfish policy and player actions across regions. | Fire Simulation / Reputation System | High |
| **REQ-008** | The system shall track and display city reputation based on player actions. | Reputation Module | Medium |
| **REQ-009** | The system shall unlock better tools, resources, and optional multiplayer collaboration after reaching a progression threshold. | Progression Module / Multiplayer Module | Low (Optional) |
| **REQ-010** | The system shall synchronize shared state across all players in multiplayer mode. | Multiplayer Backend / Database | Low (Optional) |
| **REQ-011** | The system shall allow players to implement disaster policies such as seasonal fire bans, evacuation protocols, etc. | Policy Management Module | High |
| **REQ-012** | The system shall calculate the effects of disaster policies on fire frequency, fire spread rate, resource usage, and regional damage. | Policy Management Module / Fire Simulation Engine | High |
| **REQ-013** | The system shall escalate disaster difficulty over time by increasing fire frequency, fire spread rate, and overlapping disaster events in proportion to player progression levels, unlocking new challenges and tools. | Fire Simulation Engine | High |
| **REQ-014** | The system shall allow players to provide aid, resources, or emergency support to other regions in multiplayer mode, affecting fire outcomes and reputation. | Multiplayer Backend / Deployment Module | Low (Optional) |
| **REQ-015** | The system shall provide real-time feedback on the impact of policy decisions, including resource usage, SDG progress, environmental loss, and reputation changes. | UI Module / Scoring System | Medium |

### 3.3 Non-Functional Requirements (NFRs)
Non-functional requirements describe how the system behaves; performance, usability, reliability, and constraints. The Non-Functional Requirements include:

**Performance**
- The system shall maintain at least 30 frames per second on supported platforms (Windows, macOS).
- The system shall update fire spread and resource status in real-time without noticeable delay (< 1 second).

**Usability**
- The system shall provide a clear and intuitive UI for resource management, city status, and disaster alerts.
- The system shall be suitable for players aged 13 and above, with family-friendly content.

**Reliability and Robustness**
- The system shall save player progression and reputation data to the PostgreSQL database to prevent loss on crash or restart.
- The system shall handle multiple simultaneous events without crashing or freezing.

**Educational Effectiveness**
- The system shall clearly show the consequences of player actions on fire spread and environmental damage.
- The system shall reinforce cooperation and resource sharing by providing measurable feedback on outcomes.

**Scalability (for multiplayer) (optional)**
- The system shall allow up to X players per session (you can define a reasonable number for a school project, e.g., 4–6).
- The system shall synchronize events and shared state consistently across all clients.

---

## 4. High-Level Overview of User Interface
The user interface centers on a dynamic map that visually communicates the location and progression of wildfires in real time. Players can monitor fires, city status, and environmental impact directly from the map. Dedicated dashboard panels allow players to efficiently manage budgets, allocate emergency services, and implement policy decisions under strict time pressure. A collaborative aid menu enables players to send firefighters, equipment, emergency support, or funding to neighboring cities, emphasizing cooperation and strategic planning. The interface is designed to be intuitive, family-friendly, and educational, providing clear visual and textual feedback on the consequences of player actions.

---

## 5. Use Cases

### Use Case: Start / End Game
- **Precondition:** Player has installed or launched the game.
- **Actor:** Player
- **Main Scenario:**
  1. Player selects “New Game” or “Exit” from the main menu.
  2. System loads initial map, cities, resources, and tutorial (if starting a new game).
  3. System prompts to save progress when exiting.
  4. System closes all processes and returns the player to desktop or menu.
- **Alternative Scenario:**
  - If loading or exiting fails, system shows an error and allows retry or cancellation.

### Use Case: Load / Save Game
- **Precondition:** Player has previously saved a game or is at a point where saving is allowed.
- **Actor:** Player
- **Main Scenario:**
  1. Player selects “Load Game” or “Save Game” from the menu.
  2. System retrieves or writes game state, including map, fires, resources, and progression.
  3. System confirms success to the player.
- **Alternative Scenario:**
  - If the saved file is missing or corrupted, system notifies the player and offers to start a new game or retry saving.
  - If saving fails due to disk/database issues, system allows retry.

### Use Case: Pause / Resume Game
- **Precondition:** Game is running.
- **Actor:** Player
- **Main Scenario:**
  1. Player selects “Pause” from the in-game menu.
  2. System stops time-sensitive processes such as fire spread, timers, and events.
  3. Player resumes game; system restores all paused processes to their previous state.
- **Alternative Scenario:**
  - If pause or resume fails, the system continues the game and notifies the player.

### Use Case: View City/Region Status Dashboard
- **Precondition:** Player has access to the dashboard panel.
- **Actor:** Player
- **Main Scenario:**
  1. Player opens the dashboard.
  2. System displays fire spread, resource levels, city/region metrics, environmental impact, and containment status.
- **Alternative Scenario:**
  - If dashboard fails to load, system shows an error and allows retry.

### Use Case: Allocate Budget
- **Precondition:** Player has a total available budget; cities exist in the region.
- **Actor:** Player
- **Main Scenario:**
  1. Player opens the budget allocation panel.
  2. Player distributes funds among firefighting, emergency services, and infrastructure repair.
  3. System updates budget counters and city readiness metrics.
  4. System enforces limits and confirms allocations.
- **Alternative Scenario:**
  - If player attempts to allocate more than the available budget, system rejects the allocation and prompts correction.

### Use Case: Deploy Fire Management Response
- **Precondition:** A city is on fire. Player has available units and equipment.
- **Actor:** Player
- **Main Scenario:**
  1. Player selects the city on the map.
  2. Player chooses how many firefighting units and equipment to deploy.
  3. System updates fire intensity and city status based on deployed resources.
  4. System deducts used resources from player inventory.
  5. System updates reputation based on timely or cooperative response.
- **Alternative Scenario:**
  - If no units or equipment are available, system shows a warning.
  - Player may wait for replenishment or allocate funds to acquire more resources.

### Use Case: Provide Aid to Neighboring City and Region
- **Precondition:** Neighboring city is under threat; player has available units or funds; progression level allows assistance.
- **Actor:** Player
- **Main Scenario:**
  1. Player selects a neighboring city on the map.
  2. Player chooses an assistance type (firefighting units, equipment, emergency support, or funds).
  3. System calculates impact on fire containment and environmental damage.
  4. System updates both sending and receiving city status.
  5. System adjusts reputation and awards progression points.
- **Alternative Scenario:**
  - If insufficient resources exist, system prevents action and suggests alternatives (e.g., send fewer units or provide funds only).

### Use Case: View Reputation / Progression
- **Precondition:** Player has taken actions affecting cities or regions.
- **Actor:** Player / System
- **Main Scenario:**
  1. System calculates reputation changes based on player actions, cooperation, and policy decisions.
  2. System updates progression points and displays them on the UI.
- **Alternative Scenario:**
  - Repeated selfish or delayed decisions decrease reputation and may trigger cascading failures.

### Use Case: Implement Regional Policy
- **Precondition:** Player has reached progression level allowing policy management; regions are unlocked.
- **Actor:** Player
- **Main Scenario:**
  1. Player selects a policy type (fire ban, prescribed burn, evacuation, defensible space, etc.).
  2. System applies policy effects to selected regions.
  3. System updates fire spread probability, resource usage, and potential environmental loss.
  4. System adjusts reputation and SDG/progression metrics based on outcomes.
- **Alternative Scenario:**
  - If policy cannot be applied due to resource limits or progression, system prevents action and provides feedback.

### Use Case: Receive Environmental Alerts
- **Precondition:** A disaster occurs or risk indicators rise in a city or region.
- **Actor:** System
- **Main Scenario:**
  1. System sends alerts to the player, detailing affected cities or regions and recommended actions.
  2. System updates risk levels and displays warnings on the UI.
- **Alternative Scenario:**
  - Player ignores alerts; system continues tracking fire spread and risk without player intervention.
