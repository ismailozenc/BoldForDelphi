namespace Bold.PortBrowser;

/// <summary>Where the browser has been: back and forward through the nodes that were shown.</summary>
public sealed class History
{
    private readonly List<BrowserNode> _visited = [];
    private int _position = -1;

    public bool CanGoBack => _position > 0;

    public bool CanGoForward => _position < _visited.Count - 1;

    public BrowserNode? Current => _position >= 0 ? _visited[_position] : null;

    /// <summary>A node was shown: what was ahead is forgotten. Showing the current node again changes nothing.</summary>
    public void Visit(BrowserNode node)
    {
        if (Current == node)
        {
            return;
        }

        _visited.RemoveRange(_position + 1, _visited.Count - _position - 1);
        _visited.Add(node);
        _position++;
    }

    public BrowserNode? Back() => CanGoBack ? _visited[--_position] : null;

    public BrowserNode? Forward() => CanGoForward ? _visited[++_position] : null;

    public void Clear()
    {
        _visited.Clear();
        _position = -1;
    }
}
