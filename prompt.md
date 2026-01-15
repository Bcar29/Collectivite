Tu es un développeur C# senior spécialisé en sécurité logicielle offline.

Contexte :
- Projet existant : application WPF en C# (.NET 8)
- Logiciel 100 % offline
- Base de données MySQL locale
- Pas de serveur, pas d’internet
- Objectif : bloquer totalement le logiciel après une durée définie (ex: 180 jours)
- L’utilisateur peut modifier la date Windows, copier les fichiers, etc.

Je veux que tu implémentes un système de protection OFFLINE complet basé sur 5 étapes combinées :

1) Date d’installation
   - Enregistrer la date du premier lancement
   - Utiliser exclusivement DateTime.UtcNow
   - Stocker la date de façon persistante et chiffrée
   - Stockage principal : registre Windows (HKCU)
   - Stockage secondaire (fallback) : fichier caché dans AppData
   - Si les deux valeurs sont absentes ou incohérentes → blocage

2) Expiration par durée
   - Expiration = InstallDate + N jours (ex: 180)
   - Ne jamais stocker directement la date d’expiration
   - Recalculer à chaque lancement
   - Si durée dépassée → blocage

3) Détection de manipulation de la date système
   - Stocker LastRunDate (UTC, chiffrée)
   - À chaque lancement :
       - Si DateTime.UtcNow < LastRunDate → triche → blocage immédiat
   - Mettre à jour LastRunDate uniquement après toutes les vérifications réussies

4) Empreinte machine (anti-copie)
   - Générer un MachineFingerprint stable à partir de :
       - CPU ID
       - Serial disque système
   - Chiffrer et stocker cette empreinte
   - Si l’empreinte change → blocage

5) Chiffrement et obfuscation
   - Utiliser AES pour chiffrer toutes les données sensibles :
       - InstallDate
       - LastRunDate
       - MachineFingerprint
   - Clé dérivée (PBKDF2) et non stockée en clair
   - Aucun texte sensible en clair dans le registre ou les fichiers

Contraintes techniques :
- Code compatible .NET 8
- Architecture propre (services / helpers)
- Aucun code dans le code-behind UI
- Une seule classe centrale : LicenseGuard ou LicenseManager
- Méthode principale :
    bool CheckLicense(out string blockReason)

Comportement attendu :
- Au démarrage de l’application, appeler CheckLicense
- Si false :
    - bloquer totalement l’UI
    - afficher un message simple “Licence expirée ou invalide”
    - fermer proprement l’application

Livrables attendus :
- Classes C# complètes et compilables
- Méthodes clairement nommées
- Commentaires expliquant la logique (pas des commentaires évidents)
- Exemple d’intégration dans App.xaml.cs ou MainWindow
- Aucun code factice ou pseudo-code

Important :
- Ne PAS utiliser internet
- Ne PAS utiliser de serveur
- Ne PAS simplifier la logique
- Implémentation réaliste, comme pour un vrai logiciel commercial offline
