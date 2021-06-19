# APPLE Health XML export to SQLITE

## This project is not complete

### Overview

This project converts the somewhat useless massive XML file to a slightly less useless and slightly less massive sqlite file. Please note that Metadata nodes (child nodes) in the XML file are entered into seperate tables, where they join to their parent table on rowid and parentTableName

### Example SQL Queries

`.tables` will show all tables

`.schema {table}` will show the schema information for a given table

`SELECT * FROM FlightsClimbed limit 10` Returns 10 records from FlightsClimbed

`SELECT Sum(value), date(creationDate) as dte from FlightsClimbed GROUP BY dte;` Returns the number of flights climbed on a date (should work, but doesn't for me)

`SELECT MAX(value) FROM HeartRate;` Returns the highest heartrate recorded

`SELECT * FROM BodyMass JOIN MetaDataEntry on BodyMass.ROWID = MetaDataEntry.FK and MetaDataEntry.tableReference = 'BodyMass' limit 10` Will return the first 10 BodyMass entries and it's corresponding metadata