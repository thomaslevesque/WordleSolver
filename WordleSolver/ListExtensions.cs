namespace WordleSolver;

static class ListExtensions
{
    public static T PickRandom<T>(this IReadOnlyList<T> list, Random? random = null)
    {
        random ??= Random.Shared;
        int index = random.Next(0, list.Count);
        return list[index];
    }

    public static T PickRandomWeighted<T>(this IReadOnlyList<T> list, Func<T, int> weightFunction, Random? random = null)
    {
        random ??= Random.Shared;
        var listWithWeights = list.Select(item => (Item: item, Weight: weightFunction(item))).ToList();
        var totalWeight = listWithWeights.Sum(i => i.Weight);
        var rnd = random.Next(totalWeight);
        foreach (var (item, weight) in listWithWeights)
        {
            if (rnd < weight)
                return item;
            rnd -= weight;
        }
        int index = random.Next(0, list.Count);
        return list[index];
    }
}