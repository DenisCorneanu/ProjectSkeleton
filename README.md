# Minefield Explorer

Minefield Explorer este un joc simplu de tip Minesweeper, realizat in C# si .NET folosind SDL.

Jucatorul trebuie sa descopere celulele sigure si sa evite minele ascunse. Click stanga descopera o celula, iar click dreapta pune sau scoate un steag. Jocul se termina atunci cand jucatorul descopera o mina sau cand toate celulele sigure au fost descoperite.

Jocul are scor si salveaza cele mai bune scoruri intre rulari.

## Cum se ruleaza


dotnet run 


## Controale
* Click stanga: descopera o celula
* Click dreapta: pune sau scoate un steag
* R: restart
* Escape sau Q: iesire din joc
* Butonul R din stanga sus poate fi apasat pentru restart


## Reguli

Tabla are dimensiunea 10x10 si contine 15 mine ascunse.

Jucatorul castiga daca descopera toate celulele care nu contin mine.
Jucatorul pierde daca descopera o mina.

Numarul de steaguri este limitat la numarul de mine, deci pot fi puse maximum 15 steaguri.

## Scor si high-score

Scorul creste atunci cand sunt descoperite celule sigure.

Cele mai bune scoruri sunt salvate in fisierul highscores.json si sunt incarcate automat cand jocul porneste din nou.

## Structura proiectului

Proiectul foloseste skeleton-ul SDL primit la laborator. Codul este impartit in cateva clase simple:

Program.cs porneste jocul.
Engine.cs controleaza loop-ul principal, input-ul, statusul jocului, restartul si actualizarea scorului.
Board.cs contine logica tablei de Minesweeper.
GameRenderer.cs deseneaza tabla, minele, steagurile, numerele, butonul de restart si counter-ul de steaguri.
HighScoreService.cs salveaza si incarca scorurile folosind JSON.

## Functionalitati C#/.NET folosite

In proiect sunt folosite mai multe concepte lucrate pe parcursul laboratoarelor:

* clase si enum-uri pentru organizarea logicii
* record struct pentru pozitiile de pe tabla
* LINQ pentru gestionarea scorurilor
* async/await pentru salvarea si incarcarea scorurilor
* IDisposable pentru eliberarea resurselor SDL
* serializare JSON pentru high-score
