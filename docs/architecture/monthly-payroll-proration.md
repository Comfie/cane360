# Monthly payroll proration

Monthly work records retain the effective-date monthly rate snapshot. The work
record itself has no amount because earnings depend on the payroll period and
all eligible evidence in that period.

For each worker and monthly rate snapshot, payroll counts distinct dates with
present attendance and supervisor and manager confirmed work evidence. An
active duplicate monthly work record for the same worker and date blocks both
records. Each eligible date earns one share of the rate, using the number of
calendar days in that payroll month as the denominator. Missing or absent days
earn no share. The total cannot exceed one monthly rate for that snapshot.

Each evidence line remains separately traceable and consumable. Cumulative
rounding allocates cents in date order so a fully verified month totals the
exact monthly rate. Calculations include a versioned calendar-day policy token
in their source fingerprint; Grower approval recalculates and compares the
authoritative evidence and totals. Rates too small to allocate one cent to each
verified day are blocked rather than silently rounded to zero.

This rule is the approved first implementation for calendar-day proration.
Paid leave and other non-work entitlements need their own approved source and
calculation rule before they can contribute payable days.
