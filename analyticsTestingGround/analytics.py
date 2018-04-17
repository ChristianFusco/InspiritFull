import pandas as pd
import numpy as np
import random
from sklearn.metrics import roc_curve, auc
from sklearn.ensemble import RandomForestClassifier
from sklearn.ensemble import GradientBoostingClassifier
from sklearn import ensemble
from sklearn.metrics import confusion_matrix
from sklearn.metrics import accuracy_score
from sklearn.metrics import mean_squared_error
from sklearn.preprocessing import LabelEncoder
import matplotlib.pyplot as plt

"""
This is the code we used to get the ROC curve, accuracy, etc.
Mucho credit to David Jordan from that one DAT class I took with him.
"""


df = pd.read_csv("data.csv", names = ["Heartrate", "Temperature", "MYO", "GSR", "SPO2", "Timestamp", "Stressed"])
accuracy=0
mean_squared=0
trials = 1
for i in range(trials):
        

    def some(x, n):
        return x.ix[random.sample(set(x.index), n)]

    stressed_df = df[df['Stressed']==True]
    print(stressed_df.mean())
    nonstressed_df = df[df['Stressed']==False]
    print(nonstressed_df.mean())
    n_samples = min(stressed_df.shape[0], nonstressed_df.shape[0])

    df = some(stressed_df, n_samples)
    df = df.append(some(nonstressed_df, n_samples))

    df['is_train'] = np.random.uniform(0, 1, len(df)) <= .75

    Train, Validate = df[df['is_train']==True], df[df['is_train']==False]

    print('Training set size =', Train.shape[0])
    print('Validation set size =', Validate.shape[0])

    features = list(set(list(df.columns)) - set(['Timestamp','is_train','Stressed']))

    # Written by: Matt
    x_train = Train[list(features)].values
    y_train = Train['Stressed'].values
    x_validate = Validate[list(features)].values
    y_validate = Validate['Stressed'].values
    print(y_train)

    # Written by: Matt
    # Create the random forest classifier
    random.seed(100)
    rf = RandomForestClassifier(n_estimators=100)
    rf.fit(x_train, y_train)

    # Written by: Matt
    # Predict the probablies of recalls for the validation set.
    status = rf.predict_proba(x_validate)

    # Written by: Matt
    # Calculate the ROC curve and Area under the curve.
    fpr, tpr, _ = roc_curve(y_validate, status[:,1])
    roc_auc = auc(fpr, tpr)
    print('Area under ROC curve =', roc_auc)


    y_true = y_validate

    # Get a list of the predicted recalled states.
    # The status array holds probabilities whether something is recalled
    y_pred = np.array([(item[1] >= 0.5) for item in status]).astype(int)

    # Calculate the confusion matrix
    cnf_matrix = confusion_matrix(y_true, y_pred)

    # Print the confusion matrix
    print('confusion matrix:')
    print(cnf_matrix)

    # Now print out what it MEANS:
    print()
    print(cnf_matrix[0][0], ' non-recalled cars were predicted to be not recalled')
    print(cnf_matrix[0][1], ' non-recalled cars were predicted to be recalled')
    print(cnf_matrix[1][0], ' recalled cars were predicted to be not recalled')
    print(cnf_matrix[1][1], ' recalled cars were predicted to be recalled')

    # Written by: David
    accuracy +=  accuracy_score(y_true, y_pred)
    mean_squared += mean_squared_error(y_true, y_pred)
    print('accuracy =', accuracy_score(y_true, y_pred))
    print('Mean squared error =', mean_squared_error(y_true, y_pred))

print(accuracy/trials)
print(mean_squared/trials)

# Written by: Matt
# Plot the ROC curve
plt.figure()
lw = 2
plt.plot(fpr, tpr, color='darkorange',
         lw=lw, label='ROC curve (area = %0.2f)' % roc_auc)
plt.plot([0, 1], [0, 1], color='navy', lw=lw, linestyle='--')
plt.xlim([0.0, 1.0])
plt.ylim([0.0, 1.05])
plt.xlabel('False Positive Rate')
plt.ylabel('True Positive Rate')
plt.title('Receiver operating characteristic example')
plt.legend(loc="lower right")
plt.savefig("fig.png")