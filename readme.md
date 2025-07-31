

## Purchase Order Request Agent - Updated for Container Apps Deployment

Advanced Agentic Demo Flow (ReACT Style)


Perfect choice. Invoice processing hits the sweet spot:

Universally relatable,

Clear steps with branching logic, and

A great setup for evolving into a multi-agent architecture later.



🔁 Cognitive Loop Implementation
Reads the next step from a structured prompt plan

Invokes the tool

Updates memory (step status)

Reflects → Decides next step

Exits when plan complete or error blocks progress


🧾 Sample Invoice 1 – Small, Auto-Approved
{
  "invoiceNumber": "INV-1001",
  "vendor": "Acme Supplies",
  "amount": 4200,
  "dueDate": "2025-07-31",
  "lineItems": [
    { "description": "Printer paper", "quantity": 10, "unitPrice": 30 },
    { "description": "Staplers", "quantity": 5, "unitPrice": 40 }
  ]
}

🧾 Sample Invoice 2 – Large, Needs Approval
json
Copy
Edit
{
  "invoiceNumber": "INV-1002",
  "vendor": "Global Tech Services",
  "amount": 72000,
  "dueDate": "2025-08-15",
  "lineItems": [
    { "description": "Consulting – July", "quantity": 1, "unitPrice": 72000 }
  ]
}

🧾 Sample Invoice 3 – Unapproved Vendor
json
Copy
Edit
{
  "invoiceNumber": "INV-1003",
  "vendor": "Unknown Vendor LLC",
  "amount": 15000,
  "dueDate": "2025-07-20",
  "lineItems": [
    { "description": "Unlisted service", "quantity": 1, "unitPrice": 15000 }
  ]
}






User Input:

Start: “Dallas, TX”

Destinations: “Plano, TX”, “Irving, TX”, “Fort Worth, TX”

Agent Step 1:

Calls Route/Delivery Optimizer

Gets: OrderedStops, EstimatedDistance, EstimatedDuration, MapUrl

Agent Step 2:

Calls Weather tool for each stop, using estimated arrival times

Agent Step 3:

Calls Traffic tool for the route at suggested departure

Agent Step 4:

Reflects:

“Should I recommend a later departure or reordering stops to avoid bad weather/traffic?”

(For the demo, you can keep it simple: just show results, or suggest, ‘Consider leaving at 10:30am to avoid rain at Plano.’)



### Prompts

 - “Plan my service route for today’s jobs, let me know if there are any weather or traffic issues, and remind me if I have any outstanding compliance checks. If I’m delayed, notify the customers, and if something goes wrong, help me file an incident report or call for field support.”



Demo Toolset
Route/Delivery Optimizer
Plans the most efficient route and sequence for deliveries/service calls.

Traffic/Window Analyzer
Finds the best departure time or delivery window to minimize delays.

Route Risk Checker
Assesses weather and road hazards along the planned route.

Customer Notification Tool
Sends ETAs, reminders, or delay notifications to customers at each site.

Compliance/Checklist Validator
Ensures all required compliance, safety checks, or paperwork are completed before and during the trip.

Incident Report Generator
Enables quick logging and reporting of delivery/service issues, incidents, or damage.

Field Support Dispatcher
Requests urgent field support or roadside assistance as needed during operations.


Summary Table (for quick reference):
Tool	Class Name
Route/Delivery Optimizer	RoutePlan
Traffic/Window Analyzer	TrafficWindow
Route Risk Checker	RouteRiskAssessment
Customer Notification Tool	CustomerNotificationResult
Compliance/Checklist Validator	ComplianceCheckResult
Incident Report Generator	IncidentReport
Field Support Dispatcher	SupportDispatch