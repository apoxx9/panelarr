// Publisher-primary ordering for the grouped series index. Stable, so the
// user's chosen sort is preserved within each publisher group. Series
// without a publisher collect at the end.
export default function sortSeriesByPublisher(items) {
  return [...items].sort((a, b) => {
    const publisherA = (a.publisherName || '').toLowerCase();
    const publisherB = (b.publisherName || '').toLowerCase();

    if (publisherA === publisherB) {
      return 0;
    }

    if (!publisherA) {
      return 1;
    }

    if (!publisherB) {
      return -1;
    }

    return publisherA < publisherB ? -1 : 1;
  });
}
