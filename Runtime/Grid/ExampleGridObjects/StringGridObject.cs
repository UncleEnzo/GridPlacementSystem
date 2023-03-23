namespace Nevelson.GridPlacementSystem
{
    public class StringGridObject
    {
        Grid<StringGridObject> grid;
        int x;
        int y;
        string letters;
        string numbers;

        public StringGridObject(Grid<StringGridObject> grid, int x, int y)
        {
            this.grid = grid;
            this.x = x;
            this.y = y;
            this.letters = "";
            this.numbers = "";
        }

        public void AddLetter(string letter)
        {
            letters += letter;
            grid.TriggerGridObjectChanged(x, y);
        }

        public void AddNumber(string number)
        {
            numbers += number;
            grid.TriggerGridObjectChanged(x, y);
        }

        public override string ToString()
        {
            return letters + "\n" + numbers;
        }
    }
}