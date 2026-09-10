namespace MyShop.Domain.Catalog;

public sealed class Category
{
    private Category(string name, CategoryId? parentCategoryId)
    {
        Id = CategoryId.New();
        Name = ValidateName(name);
        ParentCategoryId = parentCategoryId;
    }

    private Category(CategoryId id, string name, CategoryId? parentCategoryId)
    {
        Id = id;
        Name = name;
        ParentCategoryId = parentCategoryId;
    }

    public CategoryId Id { get; }
    public string Name { get; private set; }
    public CategoryId? ParentCategoryId { get; private set; }
    public bool IsRoot => ParentCategoryId is null;

    public static Category CreateRoot(string name) => new(name, null);

    public static Category CreateChild(string name, CategoryId parentCategoryId)
    {
        ValidateParentCategoryId(parentCategoryId);
        return new Category(name, parentCategoryId);
    }

    internal static Category Rehydrate(
        CategoryId id,
        string name,
        CategoryId? parentCategoryId)
    {
        if (id == default)
            throw new ArgumentException("Category ID must not be empty.", nameof(id));
        if (parentCategoryId == default(CategoryId))
            throw new ArgumentException("Parent category ID must not be empty.", nameof(parentCategoryId));
        if (parentCategoryId == id)
            throw new ArgumentException("A category cannot be its own parent.", nameof(parentCategoryId));

        return new Category(id, ValidateName(name), parentCategoryId);
    }

    public void Rename(string name) => Name = ValidateName(name);

    public void MoveUnder(CategoryId parentCategoryId)
    {
        ValidateParentCategoryId(parentCategoryId);

        if (parentCategoryId == Id)
            throw new ArgumentException("A category cannot be its own parent.", nameof(parentCategoryId));

        ParentCategoryId = parentCategoryId;
    }

    public void MoveToRoot() => ParentCategoryId = null;

    private static string ValidateName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name must not be empty or whitespace.", nameof(name));
        return name;
    }

    private static void ValidateParentCategoryId(CategoryId parentCategoryId)
    {
        if (parentCategoryId == default)
            throw new ArgumentException("Parent category ID must not be empty.", nameof(parentCategoryId));
    }
}
