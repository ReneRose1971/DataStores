# Sicherheitsrichtlinie

## Unterstützte Versionen

Wir unterstützen derzeit folgende Versionen mit Sicherheitsupdates:

| Version | Unterstützt          |
| ------- | -------------------- |
| 1.0.x   | Ja                   |
| < 1.0   | Nein                 |

## Sicherheitslücke melden

**Bitte melden Sie Sicherheitslücken NICHT über öffentliche GitHub Issues.**

Wenn Sie eine Sicherheitslücke entdecken, senden Sie bitte eine E-Mail an den Projektmaintainer.

### Was sollte der Bericht enthalten?

Bitte geben Sie so viele der folgenden Informationen wie möglich an:

- **Art der Sicherheitslücke** (z.B. Injection, Race Condition, Denial of Service, etc.)
- **Vollständige Pfade** der betroffenen Quelldateien
- **Standort** des betroffenen Quellcodes (Tag/Branch/Commit oder direkte URL)
- **Spezielle Konfiguration**, die erforderlich ist, um das Problem zu reproduzieren
- **Schritt-für-Schritt-Anleitung** zur Reproduktion des Problems
- **Proof-of-Concept oder Exploit-Code** (falls möglich)
- **Auswirkung** der Sicherheitslücke, einschließlich wie ein Angreifer die Lücke ausnutzen könnte

Diese Informationen helfen uns, Ihren Bericht schneller zu bearbeiten.

## Reaktionszeit

Wir bemühen uns:

- Innerhalb von **48 Stunden** auf Ihren Bericht zu antworten
- Innerhalb von **7 Tagen** eine erste Einschätzung der Schwere zu geben
- Sie über den **Fortschritt** zur Behebung auf dem Laufenden zu halten

## Offenlegungsrichtlinie

Wenn Sie eine Sicherheitslücke melden:

1. Geben Sie uns eine angemessene Zeit zur Behebung (mindestens 90 Tage)
2. Wir werden mit Ihnen zusammenarbeiten, um das Problem zu verstehen und zu beheben
3. Wir werden Sie über den Fortschritt informieren
4. Nach der Veröffentlichung eines Patches können wir gemeinsam die Details offenlegen
5. Wir würdigen Ihre Entdeckung gerne (falls gewünscht)

## Bevorzugte Sprachen

Wir bevorzugen alle Kommunikation auf **Deutsch** oder **Englisch**.

## Sicherheits-Best-Practices für Benutzer

### Bei Verwendung von DataStores

1. **Persistierung**
   - Verwenden Sie verschlüsselte Speicherstrategien für sensible Daten
   - Stellen Sie sicher, dass Persistierungs-Dateien angemessen geschützt sind
   - Implementieren Sie Zugriffskontrolle auf Persistierungs-Speicher

2. **Dependency Injection**
   - Registrieren Sie Stores nur in vertrauenswürdigen Registraren
   - Validieren Sie Eingaben vor dem Speichern in Stores

3. **Thread-Sicherheit**
   - DataStores ist thread-sicher, aber Ihre Geschäftslogik muss es ebenfalls sein
   - Seien Sie vorsichtig bei der Verwendung von Events in Multi-Thread-Umgebungen

4. **Serialisierung**
   - Verwenden Sie sichere Serialisierungsbibliotheken
   - Validieren Sie deserialisierte Daten
   - Achten Sie auf Deserialisierungs-Angriffe bei Custom IPersistenceStrategy

### Bekannte Sicherheitsüberlegungen

#### Auto-Save in PersistentStoreDecorator

**Verhalten**: Fehler beim Speichern werden stumm behandelt (Fire-and-Forget).

**Risiko**: Datenverlust bei Speicherproblemen wird nicht gemeldet.

**Empfehlung**: 
```csharp
// Implementieren Sie Logging in Produktionsumgebungen
public class SecurePersistenceStrategy<T> : IPersistenceStrategy<T>
{
    private readonly ILogger _logger;
    
    public async Task SaveAllAsync(IReadOnlyList<T> items, CancellationToken ct)
    {
        try
        {
            // Speicher-Logik
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kritischer Speicherfehler");
            // Optional: Benachrichtigung, Retry, etc.
            throw; // In sicherheitskritischen Szenarien
        }
    }
}
```

#### Race Conditions

**Geschützt**: DataStores verwendet Locks und Semaphores für Thread-Sicherheit.

**Nicht geschützt**: 
- Operationen über mehrere Stores hinweg
- Geschäftslogik in Event-Handlern

**Empfehlung**: Implementieren Sie eigene Synchronisation für komplexe Workflows.

## Sicherheits-Audit

Dieses Projekt wurde **nicht** durch ein professionelles Sicherheits-Audit überprüft.

Wir freuen uns über Security-Reviews und nehmen Sicherheits-Feedback ernst.

## Hall of Fame

Wir danken folgenden Sicherheitsforschern für verantwortungsvolle Offenlegung:

*(Noch keine Einträge)*

---

**Letzte Aktualisierung**: Januar 2025
