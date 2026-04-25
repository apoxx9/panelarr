import _ from 'lodash';
import { update } from 'Store/Actions/baseActions';

function updateIssues(section, issues, issueIds, options) {
  const data = _.reduce(issues, (result, item) => {
    if (issueIds.indexOf(item.id) > -1) {
      result.push({
        ...item,
        ...options
      });
    } else {
      result.push(item);
    }

    return result;
  }, []);

  return update({ section, data });
}

export default updateIssues;
