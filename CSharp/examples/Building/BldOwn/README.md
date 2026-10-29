# Buildings and Owners

Delphi Bold's classic Buildings and Owners example (`examples/Delphi/Compound/Building`, `BldOwn.dpr`) on WinForms. It
runs on the Building model and its classes ([`../BuildingClasses`](../BuildingClasses)) and keeps its objects in a
SQLite database (the Delphi example used InterBase).

| File | Delphi | What |
|---|---|---|
| `BldOwn.cs` | `BldOwn.dpr` | The program: the data module, then the main form. |
| `Datamod.cs` | `Datamod.pas` | The model, the system and its persistence, the renderers, the filter and the comparer of persons, and the form plugged in for persons. |
| `Mainform.cs` | `Mainform.pas` | The main form. |
| `PersonAutoFormUnit.cs` | `PersonAutoFormUnit.pas` | A person's own form. |

The forms are built in code with the names of the Delphi form's components, and the event handlers keep the Delphi
names. The buttons are linked to Bold's actions with `TBoldActionLink`; the Create DB button runs a
`TBoldGenerateSchemaAction` where the Delphi example had an InterBase database action.

## Run it

On Windows:

```
dotnet run --project CSharp/examples/Building/BldOwn [database file]
```

The database is `bldown.db` in the current folder unless another file is given.

1. The first time, click **Create DB** to create the database, then **Open system**.
2. Right-click the person grid and choose **new** (or press Insert) for a new person. A new person gets a name and
   assets (`TPerson.CompleteCreate`). **Rich** is checked for assets over 10,000 (the data module's `IsRichRenderer`);
   the assets show in red when negative and in blue otherwise (`NegativeRedRenderer`). **Rich only** and
   **Alphabetically** filter and sort the persons.
3. Right-click the buildings grid on either page for a new building. An address containing "Bold" shows bold on aqua,
   "Rose" in red and "Select" in green (`bsrAddress`).
4. Drag persons from the person grid to the owners and residents list boxes. The Rent/Resident column shares the rent
   among the residents (`bsrRentPerResident`, which also lets you edit it); the Rent column shows a rent of 1500 or
   more in red on silver (`HighRentRenderer`).
5. **Charge Rent** moves the rent from the residents to the owners; a resident who cannot pay is evicted.
6. **Update DB** saves. **Checkpoint**, **Undo** and **Redo** undo and redo the changes back to the last checkpoint.
7. **Show in own window** in a menu opens the current object in a form of its own: persons in the form of
   `PersonAutoFormUnit`, plugged in by the data module's `TBoldPlaceableAFP`, other objects in an auto form.

The test `BldOwnTests` in `tests/Bold.WinForms.Tests` runs the application through these steps.
