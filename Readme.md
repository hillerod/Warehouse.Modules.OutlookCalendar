# Warehouse module: OutlookCalendar

With this module, you can fetch data from Outlook Exchange calendars, to evaluate on bookings of meeting rooms. Data gets saved  into your own data warehouse on Azure.

It would have been obvious to use MSGraph to get the bookingdata, but because of restrictions in Hillerød municipality, bookingdata is being collected each day, with a PowerShell script, and saved in a csv on a FTP, that this module then fetches. This module then cleans the data and pushes it up to the datalake.

The module is build with [Bygdrift Warehouse](https://github.com/Bygdrift/Warehouse), that makes it possible to attach multiple modules within the same azure environment, that can collect data from all kinds of services, in a cheap data lake and database.
The data data lake, is structured as a Common Data Model (CDM), which enables an easy integration to Microsoft Power BI, through Power BI Dataflows. And the Microsoft SQL database, makes it even easier to fetch data to Excel, Power BI and a lot of other systems.

# Install on Azure

All modules can be installed and facilitated with ARM templates (Azure Resource Management): [Use ARM templates to setup and maintain this module](https://github.com/hillerod/Warehouse.Modules.OutlookCalendar/blob/master/Deploy).

# License

[MIT License](https://github.com/hillerod/Warehouse.Modules.OutlookCalendar/blob/master/License.md)