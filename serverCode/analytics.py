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
import sys
import subprocess

subprocess.check_output(['python','json2csv.py'])

Train = pd.read_csv("data.csv", names = ["Heartrate", "Temperature", "MYO", "GSV", "SPO2", "Timestamp", "Stressed"])
Train = Train.fillna(Train.mean())
Validate = pd.read_csv("payload.csv", names = ["Heartrate", "Temperature", "MYO", "GSV", "SPO2", "Timestamp", "Stressed"])
Validate = Validate.fillna(0)

features = list(set(list(Train.columns)) - set(['Timestamp','is_train','Stressed', 'GSV']))

# Written by: Matt
x_train = Train[list(features)].values
y_train = Train['Stressed'].values
x_validate = Validate[list(features)].values
y_validate = Validate['Stressed'].values

# Written by: Matt
# Create the random forest classifier
random.seed(100)
rf = RandomForestClassifier(n_estimators=100)
rf.fit(x_train, y_train)

# Written by: Matt
# Predict the probablies of recalls for the validation set.
status = rf.predict_proba(x_validate)

y_true = y_validate

# Get a list of the predicted recalled states.
# The status array holds probabilities whether something is recalled
y_pred = np.array([(item[1] >= 0.5) for item in status]).astype(int)

# Calculate the confusion matrix
cnf_matrix = confusion_matrix(y_true, y_pred)
stressed_events = 0
try:
    stressed_events+=cnf_matrix[0][1]
except:
    stressed_events+=0
try:
    stressed_events+=cnf_matrix[1][1]
except:
    stressed_events+=0
print(str(stressed_events) + "\n" + str(status[0][1]))
sys.stdout.flush()