export interface CategoryOption {
  id: string;
  code: string;
  name: string;
  active: boolean;
  displayOrder: number;
}

export function activeCategories(categories: readonly CategoryOption[]) {
  return categories.filter(category => category.active)
    .sort((a, b) => a.displayOrder - b.displayOrder || a.name.localeCompare(b.name));
}

export function categoryLabel(categories: readonly CategoryOption[], code: string) {
  const category = categories.find(candidate => candidate.code === code);
  return category ? `${category.name}${category.active ? '' : ' (inactive)'}` : code;
}
