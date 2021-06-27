# Apple Health XML export to SQLITE

## This project is not complete

### Overview

This project converts the somewhat useless massive XML file Apple gives you to a slightly less useless and slightly less massive sqlite file. Please note that Metadata nodes (child nodes) in the XML file are entered into seperate tables, where they join to their parent table on rowid and parentTableName

### Example SQL Queries

`.tables` will show all tables

`.schema {table}` will show the schema information for a given table

`SELECT * FROM FlightsClimbed limit 10` Returns 10 records from FlightsClimbed

`
SELECT
    Sum(value) as FlightsClimbed,
    date(
        datetime(
            creationDate,
            (
                CAST(CAST(timezone as INT) / 100 as TEXT) || ' hours'
            )
        )
    ) as dte
from
    FlightsClimbed
    JOIN timezone ON FlightsClimbed.ROWID = timezone.FK
    AND timezone.tableReference = 'FlightsClimbed'
GROUP BY
    dte;
` 
Returns the number of flights climbed on a date ( with timezone information)

`SELECT MAX(value) FROM HeartRate;` Returns the highest heartrate recorded

`SELECT * FROM BodyMass JOIN MetaDataEntry on BodyMass.ROWID = MetaDataEntry.FK and MetaDataEntry.tableReference = 'BodyMass' limit 10` Will return the first 10 BodyMass entries and it's corresponding metadata

`
SELECT
    BodyMass.creationDate,
    BodyMass.value as Pounds,
    Height.creationDate as hgtCRT,
    Height.value as height,
    (((BodyMass.value * 1.0) / (Height.value * 12) / (Height.value * 12)) * 703) as BMI
FROM
    BodyMass,
    Height,
    (
        SELECT
            Height.ROWID as hgtID,
            BodyMass.ROWID as bdyID,
            min(
                abs(
                    julianday(BodyMass.creationDate) - julianday(Height.creationDate)
                )
            ) as daysBetween
        from
            BodyMass,
            Height
        GROUP BY
            BodyMass.ROWID
    ) as closestDateHeight
WHERE
    BodyMass.ROWID = closestDateHeight.bdyID
    AND Height.ROWID = closestDateHeight.hgtID
ORDER BY
    BodyMass.creationDate DESC
`

Will calculate BMI for every Weight record, using the closest Height record to the BodyMass's Creation date

`
SELECT
    sum(
        Cast (
            (
                JulianDay(endDate) - JulianDay(startDate)
            ) * 24 As Integer
        )
    ) as hoursAsleep
FROM
    HKCategoryTypeIdentifierSleepAnalysis
`
Will get the total number of hours your apple watch has tracked you sleeping



