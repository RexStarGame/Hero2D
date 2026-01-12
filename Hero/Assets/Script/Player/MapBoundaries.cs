using UnityEngine;

public class MapBoundaries : MonoBehaviour
{
    [Header("Træk din 'Zone' herind")]
    // Dette er den Collider (Trigger), som definerer området, spilleren må være i.
    [SerializeField] private Collider2D walkArea;

    void LateUpdate()
    {
        // Sikkerhedstjek: Har vi husket at trække en collider ind?
        if (walkArea == null) return;

        // 1. Hent spillerens position (2D)
        Vector2 currentPos = transform.position;

        // 2. Tjek om spilleren er INDE i triggeren
        // OverlapPoint returnerer true, hvis punktet er inde i collideren.
        bool isInside = walkArea.OverlapPoint(currentPos);

        // 3. Hvis spilleren er UDENFOR...
        if (!isInside)
        {
            // ... så find det punkt på kanten af collideren, der er tættest på spilleren
            Vector2 closestPoint = walkArea.ClosestPoint(currentPos);

            // ... og flyt spilleren derhen.
            transform.position = closestPoint;
        }
    }
}