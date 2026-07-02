// Splits a publisher-ordered series list (see sortSeriesByPublisher) into
// contiguous publisher groups. Title is null for series without a publisher.
export default function groupSeriesByPublisher(items) {
  const groups = [];
  let current = null;

  items.forEach((item) => {
    const title = item.publisherName || null;

    if (!current || current.title !== title) {
      current = { title, items: [] };
      groups.push(current);
    }

    current.items.push(item);
  });

  return groups;
}
