Search by time range instead:

traces
| where message startswith "*** CUSTOM"
| where timestamp > ago(1h)
| order by timestamp desc



traces
| where message startswith "*** CUSTOM"
| summarize count() by session_Id
| order by count_ desc



Here's the query to group by session_Id and show each unique session:
This shows:
 - Each unique session_Id
 - How many custom traces per session (TraceCount)
 - When the first and last traces occurred for that session

traces
| where message startswith "*** CUSTOM"
| summarize 
    TraceCount = count(),
    FirstTrace = min(timestamp),
    LastTrace = max(timestamp)
    by session_Id
| order by LastTrace desc



Use operation_Id for request correlation:

traces
| where message startswith "*** CUSTOM"
| where operation_Id == "your-operation-id"
| order by timestamp desc



traces
| where message startswith "*** CUSTOM"
| where session_Id == "your-session-id-here"
| order by timestamp desc

Session tracking depends on your app configuration. If you're not seeing session_Id populated, you might need to use operation_Id (which correlates to individual HTTP requests) instead.